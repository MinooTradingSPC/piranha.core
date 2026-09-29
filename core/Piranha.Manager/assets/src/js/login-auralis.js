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
 * Background of the "Auralis" login design (login-auralis.scss): the
 * reference's warm gradient at the right of the page, two layers of simplex
 * noise blending a pale peach base into orange and rose, fading in from the
 * left, seen through five vertical glass slices. The script adds its own
 * container with a WebGL canvas and the slices. If WebGL isn't available it
 * removes the canvas again and the still CSS gradient on the container shows
 * through the slices instead.
 */
(function () {
    "use strict";

    var body = document.getElementById("login");
    if (!body || document.getElementById("login-aurora")) {
        return;
    }

    var aurora = document.createElement("div");
    aurora.id = "login-aurora";
    aurora.className = "login-aurora";
    aurora.setAttribute("aria-hidden", "true");

    var canvas = document.createElement("canvas");
    canvas.className = "login-aurora-canvas";
    aurora.appendChild(canvas);

    var slices = document.createElement("div");
    slices.className = "login-aurora-slices";
    for (var i = 0; i < 5; i++) {
        var slice = document.createElement("div");
        slice.className = "login-aurora-slice";
        slices.appendChild(slice);
    }
    aurora.appendChild(slices);
    body.insertBefore(aurora, body.firstChild);

    var vertexSource =
        "attribute vec2 a_position;" +
        "void main() { gl_Position = vec4(a_position, 0.0, 1.0); }";

    // The reference's shader: 2D simplex noise (Ashima Arts / Stefan
    // Gustavson, MIT) mixing the warm palette, alpha fading in from the left.
    var fragmentSource = [
        "precision highp float;",
        "uniform float u_time;",
        "uniform vec2 u_res;",

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

        "    vec3 orange = vec3(0.91, 0.34, 0.04);",
        "    vec3 rose = vec3(0.88, 0.11, 0.28);",
        "    vec3 base = vec3(1.0, 0.85, 0.65);",

        "    vec3 col = mix(base, orange, smoothstep(-0.6, 0.8, n));",
        "    col = mix(col, rose, smoothstep(-0.4, 0.9, n2) * 0.8);",

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

    var buffer = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, buffer);
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 1, -1, -1, 1, -1, 1, 1, -1, 1, 1]), gl.STATIC_DRAW);
    var position = gl.getAttribLocation(program, "a_position");
    gl.enableVertexAttribArray(position);
    gl.vertexAttribPointer(position, 2, gl.FLOAT, false, 0, 0);

    var uRes = gl.getUniformLocation(program, "u_res");
    var uTime = gl.getUniformLocation(program, "u_time");

    var reducedMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)");
    var start = performance.now();
    var frame = null;
    var lost = false;

    function resize() {
        // The gradient is soft, so it's drawn at half resolution and scaled
        // up by the browser.
        var scale = 0.5;
        var width = Math.max(1, Math.floor(canvas.clientWidth * scale));
        var height = Math.max(1, Math.floor(canvas.clientHeight * scale));

        if (canvas.width !== width || canvas.height !== height) {
            canvas.width = width;
            canvas.height = height;
            gl.viewport(0, 0, width, height);
        }
    }

    function draw(time) {
        resize();
        gl.uniform2f(uRes, canvas.width, canvas.height);
        // The reference advances time at 0.002 per millisecond.
        gl.uniform1f(uTime, time * 2);
        gl.drawArrays(gl.TRIANGLES, 0, 6);
    }

    function loop(now) {
        if (lost) {
            return;
        }
        draw((now - start) / 1000);
        frame = requestAnimationFrame(loop);
    }

    function isStill() {
        return document.hidden || (reducedMotion && reducedMotion.matches);
    }

    function run() {
        cancelAnimationFrame(frame);
        frame = null;
        if (lost) {
            return;
        }
        if (isStill()) {
            // One still frame, redrawn only when the size changes.
            draw(6);
        } else {
            frame = requestAnimationFrame(loop);
        }
    }

    canvas.addEventListener("webglcontextlost", function (e) {
        e.preventDefault();
        lost = true;
        cancelAnimationFrame(frame);
        canvas.classList.remove("is-ready");
    });

    document.addEventListener("visibilitychange", run);
    window.addEventListener("resize", function () {
        if (isStill() && !lost) {
            draw(6);
        }
    });
    if (reducedMotion && reducedMotion.addEventListener) {
        reducedMotion.addEventListener("change", run);
    }

    run();
    canvas.classList.add("is-ready");
})();
