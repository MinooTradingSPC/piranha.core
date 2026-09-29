/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

/*
 * Background of the "Lumina" login design (login-lumina.scss): the WebGL
 * cover of the "Lumina Brand Guidelines" reference, drawn inside the brand
 * card. Two layers of simplex noise drift slowly and give a slate-blue haze
 * over the dark surface, and the noise's contour lines glow in peach, faded
 * out toward the card's edges. The script adds its own canvas to the brand
 * card; if WebGL isn't available it removes it again and the CSS gradients
 * on .login-brand show through instead. It draws one still frame when the
 * visitor prefers reduced motion and stops drawing while the tab is hidden.
 */
(function () {
    "use strict";

    var host = document.querySelector("#login .login-brand");
    if (!host || document.getElementById("login-bg")) {
        return;
    }

    var canvas = document.createElement("canvas");
    canvas.id = "login-bg";
    canvas.setAttribute("aria-hidden", "true");
    host.insertBefore(canvas, host.firstChild);

    var vertexSource =
        "attribute vec2 a_position;" +
        "void main() { gl_Position = vec4(a_position, 0.0, 1.0); }";

    var fragmentSource = [
        "precision mediump float;",
        "uniform float u_time;",
        "uniform vec2 u_res;",

        "vec2 hash(vec2 p) {",
        "    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));",
        "    return -1.0 + 2.0 * fract(sin(p) * 43758.5453123);",
        "}",

        // 2D simplex noise, as in the reference.
        "float noise(in vec2 p) {",
        "    const float K1 = 0.366025404;",
        "    const float K2 = 0.211324865;",
        "    vec2 i = floor(p + (p.x + p.y) * K1);",
        "    vec2 a = p - i + (i.x + i.y) * K2;",
        "    float m = step(a.y, a.x);",
        "    vec2 o = vec2(m, 1.0 - m);",
        "    vec2 b = a - o + K2;",
        "    vec2 c = a - 1.0 + 2.0 * K2;",
        "    vec3 h = max(0.5 - vec3(dot(a, a), dot(b, b), dot(c, c)), 0.0);",
        "    vec3 n = h * h * h * h * vec3(dot(a, hash(i + 0.0)), dot(b, hash(i + o)), dot(c, hash(i + 1.0)));",
        "    return dot(n, vec3(70.0));",
        "}",

        "void main() {",
        "    vec2 uv = gl_FragCoord.xy / u_res.xy;",
        "    float t = u_time;",

        "    float n = noise(uv * 3.0 + vec2(t * 0.1, t * 0.2));",
        "    n += 0.5 * noise(uv * 6.0 - vec2(t * 0.15, 0.0));",

        // Thin contour lines where the noise crosses its levels.
        "    float lines = abs(sin(n * 10.0 + t * 0.5));",
        "    lines = pow(1.0 - lines, 4.0);",

        "    vec3 color1 = vec3(0.06, 0.09, 0.13);",
        "    vec3 color2 = vec3(0.2, 0.3, 0.5);",
        "    vec3 color3 = vec3(0.8, 0.5, 0.4);",

        "    vec3 col = mix(color1, color2, n * 0.5 + 0.5);",
        "    col += color3 * lines * 0.8;",

        "    float dist = distance(uv, vec2(0.5));",
        "    col *= smoothstep(0.8, 0.2, dist);",

        "    gl_FragColor = vec4(col, 1.0);",
        "}"
    ].join("\n");

    function giveUp() {
        if (canvas.parentNode) {
            canvas.parentNode.removeChild(canvas);
        }
    }

    var gl = canvas.getContext("webgl", { alpha: false, antialias: false });
    if (!gl) {
        giveUp();
        return;
    }

    function compile(type, source) {
        var shader = gl.createShader(type);
        gl.shaderSource(shader, source);
        gl.compileShader(shader);
        return gl.getShaderParameter(shader, gl.COMPILE_STATUS) ? shader : null;
    }

    var vertexShader = compile(gl.VERTEX_SHADER, vertexSource);
    var fragmentShader = compile(gl.FRAGMENT_SHADER, fragmentSource);
    if (!vertexShader || !fragmentShader) {
        giveUp();
        return;
    }

    var program = gl.createProgram();
    gl.attachShader(program, vertexShader);
    gl.attachShader(program, fragmentShader);
    gl.linkProgram(program);
    if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
        giveUp();
        return;
    }
    gl.useProgram(program);

    // One full-screen triangle pair.
    var buffer = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, buffer);
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 1, -1, -1, 1, -1, 1, 1, -1, 1, 1]), gl.STATIC_DRAW);
    var position = gl.getAttribLocation(program, "a_position");
    gl.enableVertexAttribArray(position);
    gl.vertexAttribPointer(position, 2, gl.FLOAT, false, 0, 0);

    var uTime = gl.getUniformLocation(program, "u_time");
    var uRes = gl.getUniformLocation(program, "u_res");

    var reducedMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var start = performance.now();
    var frame = null;
    var lost = false;

    function resize() {
        // The noise is soft, so a capped pixel ratio keeps the cost low
        // without visible loss.
        var dpr = Math.min(window.devicePixelRatio || 1, 1.5);
        var width = Math.max(1, Math.floor(canvas.clientWidth * dpr));
        var height = Math.max(1, Math.floor(canvas.clientHeight * dpr));

        if (canvas.width !== width || canvas.height !== height) {
            canvas.width = width;
            canvas.height = height;
            gl.viewport(0, 0, width, height);
        }
    }

    function draw(time) {
        resize();
        gl.uniform1f(uTime, time);
        gl.uniform2f(uRes, canvas.width, canvas.height);
        gl.drawArrays(gl.TRIANGLES, 0, 6);
    }

    function loop(now) {
        frame = null;
        if (lost || document.hidden) {
            return;
        }
        draw((now - start) / 1000);
        frame = requestAnimationFrame(loop);
    }

    function play() {
        if (frame === null && !lost && !document.hidden) {
            frame = requestAnimationFrame(loop);
        }
    }

    function pause() {
        if (frame !== null) {
            cancelAnimationFrame(frame);
            frame = null;
        }
    }

    canvas.addEventListener("webglcontextlost", function (e) {
        e.preventDefault();
        lost = true;
        pause();
        canvas.classList.remove("is-ready");
    });

    if (reducedMotion) {
        // One still frame, redrawn only when the size changes.
        draw(8);
        window.addEventListener("resize", function () { draw(8); });
    } else {
        document.addEventListener("visibilitychange", function () {
            if (document.hidden) {
                pause();
            } else {
                play();
            }
        });
        play();
    }

    canvas.classList.add("is-ready");
})();
