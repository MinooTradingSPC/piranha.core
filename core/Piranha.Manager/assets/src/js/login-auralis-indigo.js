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
 * Background of the "Auralis Indigo" login design (login-auralis-indigo.scss):
 * the reference's flowing indigo and cyan plasma, drawn by one WebGL fragment
 * shader from two layers of simplex noise and faded out toward the left. The
 * style sheet sizes the canvas to the right part of the page and lays the
 * glass slices over it. The script adds its own canvas; if WebGL isn't
 * available it removes it again and the static CSS gradient on #login shows
 * through instead. It draws one still frame when the user prefers reduced
 * motion and stops drawing while the tab is hidden.
 */
(function () {
    "use strict";

    var body = document.getElementById("login");
    if (!body || document.getElementById("login-bg")) {
        return;
    }

    var canvas = document.createElement("canvas");
    canvas.id = "login-bg";
    canvas.setAttribute("aria-hidden", "true");
    body.insertBefore(canvas, body.firstChild);

    var vertexSource =
        "attribute vec2 a_position;" +
        "void main() { gl_Position = vec4(a_position, 0.0, 1.0); }";

    var fragmentSource = [
        "precision highp float;",
        "uniform float u_time;",
        "uniform vec2 u_res;",

        // 2D simplex noise, as in the reference.
        "vec3 permute(vec3 x) { return mod(((x * 34.0) + 1.0) * x, 289.0); }",
        "float snoise(vec2 v) {",
        "    const vec4 C = vec4(0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439);",
        "    vec2 i = floor(v + dot(v, C.yy));",
        "    vec2 x0 = v - i + dot(i, C.xx);",
        "    vec2 i1 = (x0.x > x0.y) ? vec2(1.0, 0.0) : vec2(0.0, 1.0);",
        "    vec4 x12 = x0.xyxy + C.xxzz;",
        "    x12.xy -= i1;",
        "    i = mod(i, 289.0);",
        "    vec3 p = permute(permute(i.y + vec3(0.0, i1.y, 1.0)) + i.x + vec3(0.0, i1.x, 1.0));",
        "    vec3 m = max(0.5 - vec3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0);",
        "    m = m * m;",
        "    m = m * m;",
        "    vec3 x = 2.0 * fract(p * C.www) - 1.0;",
        "    vec3 h = abs(x) - 0.5;",
        "    vec3 ox = floor(x + 0.5);",
        "    vec3 a0 = x - ox;",
        "    m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);",
        "    vec3 g;",
        "    g.x = a0.x * x0.x + h.x * x0.y;",
        "    g.yz = a0.yz * x12.xz + h.yz * x12.yw;",
        "    return 130.0 * dot(m, g);",
        "}",

        "void main() {",
        "    vec2 uv = gl_FragCoord.xy / u_res.xy;",
        "    float n = snoise(uv * 2.5 + vec2(u_time * 0.4, u_time * 0.5));",
        "    float n2 = snoise(uv * 1.5 - vec2(u_time * 0.3, u_time * 0.2));",

        "    vec3 indigo = vec3(0.31, 0.27, 0.90);",
        "    vec3 cyan = vec3(0.02, 0.71, 0.83);",
        "    vec3 deep = vec3(0.15, 0.10, 0.50);",

        "    vec3 col = mix(deep, indigo, smoothstep(-0.6, 0.8, n));",
        "    col = mix(col, cyan, smoothstep(-0.4, 0.9, n2) * 0.8);",

        // Fade toward the left edge, where the page's text is.
        "    float alpha = smoothstep(-0.2, 0.6, uv.x);",
        "    gl_FragColor = vec4(col * alpha, alpha);",
        "}"
    ].join("\n");

    function giveUp() {
        if (canvas.parentNode) {
            canvas.parentNode.removeChild(canvas);
        }
    }

    var gl = canvas.getContext("webgl", { alpha: true, antialias: false, premultipliedAlpha: true });
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

    var uRes = gl.getUniformLocation(program, "u_res");
    var uTime = gl.getUniformLocation(program, "u_time");

    var reducedMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var frame = null;
    var lost = false;
    var elapsed = 0;
    var last = null;

    // The noise is smooth, so a lower resolution than the screen's looks the
    // same and costs much less.
    function resize() {
        var dpr = Math.min(window.devicePixelRatio || 1, 1.5) * 0.75;
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
        gl.uniform2f(uRes, canvas.width, canvas.height);
        // The reference advances its shader clock two units a second.
        gl.uniform1f(uTime, time * 2.0);
        gl.drawArrays(gl.TRIANGLES, 0, 6);
    }

    function loop(now) {
        if (lost) {
            return;
        }
        if (last !== null) {
            elapsed += Math.min((now - last) / 1000, 0.1);
        }
        last = now;
        draw(elapsed);
        frame = requestAnimationFrame(loop);
    }

    function start() {
        if (frame === null && !lost) {
            last = null;
            frame = requestAnimationFrame(loop);
        }
    }

    function stop() {
        if (frame !== null) {
            cancelAnimationFrame(frame);
            frame = null;
        }
    }

    canvas.addEventListener("webglcontextlost", function (e) {
        e.preventDefault();
        lost = true;
        stop();
        canvas.classList.remove("is-ready");
    });

    if (reducedMotion) {
        // One still frame, redrawn only when the size changes.
        draw(4);
        window.addEventListener("resize", function () { draw(4); });
    } else {
        document.addEventListener("visibilitychange", function () {
            if (document.hidden) {
                stop();
            } else {
                start();
            }
        });

        if (!document.hidden) {
            start();
        }
    }

    canvas.classList.add("is-ready");
})();
