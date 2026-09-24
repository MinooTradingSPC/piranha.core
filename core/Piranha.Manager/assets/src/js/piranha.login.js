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
 * Login page background: a full-screen WebGL light tunnel drawn by one
 * fragment shader - thin streaks in white, slate and terracotta that rise
 * straight behind the centre and fan out toward the viewer below the
 * horizon, with bright segments flowing down them and a soft haze, drifting
 * gently with the pointer. If WebGL isn't available the canvas stays hidden
 * and the CSS fallback on #login shows through instead.
 */
(function () {
    "use strict";

    var canvas = document.getElementById("login-bg");
    if (!canvas) {
        return;
    }

    var vertexSource =
        "attribute vec2 a_position;" +
        "void main() { gl_Position = vec4(a_position, 0.0, 1.0); }";

    var fragmentSource = [
        "#extension GL_OES_standard_derivatives : enable",
        "precision highp float;",
        "uniform vec2 u_res;",
        "uniform float u_time;",
        "uniform vec2 u_pointer;",

        "float hash(float n) { return fract(sin(n * 127.1) * 43758.5453); }",

        // Mostly white streaks, some slate blue, a few terracotta.
        "vec3 streakColor(float h) {",
        "    vec3 white = vec3(0.86, 0.89, 0.95);",
        "    vec3 slate = vec3(0.42, 0.52, 0.70);",
        "    vec3 terracotta = vec3(0.80, 0.50, 0.40);",
        "    return h < 0.55 ? white : (h < 0.8 ? slate : terracotta);",
        "}",

        // One layer of streaks: one per column, at a random offset inside
        // it. Derivatives are taken outside any branch, since they're
        // undefined in per-pixel control flow.
        "vec3 streaks(vec2 p, float x, float below, float cols, float seed, float t) {",
        "    float cx = x * cols;",
        "    float id = floor(cx) + seed * 97.0;",
        "    float w = fwidth(cx);",
        "    float h1 = hash(id * 1.31 + 0.7);",
        "    float h2 = hash(id * 2.17 + 3.1);",
        "    float h3 = hash(id * 3.73 + 9.4);",
        "    float dist = abs(fract(cx) - (0.2 + 0.6 * h2));",
        "    float core = 1.0 - smoothstep(0.0, w * 1.1 + 0.012, dist);",
        "    float halo = exp(-dist * 6.0) * 0.3;",

        // Dense and bright behind the centre, sparse toward the sides.
        "    float centre = exp(-abs(x) * 4.2);",
        "    float lit = step(0.18 + 0.8 * (1.0 - centre), h1);",

        // Each streak fades out at its own height, and flares below.
        "    float top = smoothstep(0.35 + 0.4 * h2, -0.05, p.y);",
        "    float flare = 1.0 + below * 1.8;",

        // Bright segments travelling down each streak.
        "    float seg = fract(p.y * (0.5 + h1) + t * (0.15 + 0.45 * h3) + h2 * 10.0);",
        "    float travel = 0.3 + 1.6 * pow(seg, 5.0);",

        "    return streakColor(h3) * lit * (core + halo) * travel * top * flare * (0.06 + 1.4 * centre);",
        "}",

        "void main() {",
        "    vec2 uv = (gl_FragCoord.xy - 0.5 * u_res) / u_res.y;",
        "    float t = u_time * 0.35;",
        "    vec2 p = uv + u_pointer * 0.03;",

        // Straight above the horizon, fanning out below it.
        "    float horizon = -0.08;",
        "    float below = max(horizon - p.y, 0.0);",
        "    float spread = 1.0 + below * 2.2 + below * below * 9.0;",
        "    float x = p.x / spread;",

        // Fine sharp streaks over a coarser, softer layer for depth.
        "    vec3 col = vec3(0.012, 0.014, 0.02);",
        "    col += streaks(p, x, below, 120.0, 0.0, t);",
        "    col += streaks(p, x * 1.07, below, 44.0, 1.0, t * 0.8) * 0.55;",

        // Haze behind the tunnel, a warm glow at the floor and a vignette.
        "    col += vec3(0.60, 0.63, 0.70) * exp(-abs(p.x) * 5.0) * smoothstep(0.65, -0.1, p.y) * 0.16;",
        "    col += vec3(0.80, 0.50, 0.40) * exp(-length(p - vec2(0.0, -0.6)) * 2.0) * 0.07;",
        "    col *= 1.0 - 0.5 * smoothstep(0.5, 1.3, length(uv * vec2(0.8, 1.0)));",

        "    gl_FragColor = vec4(col, 1.0);",
        "}"
    ].join("\n");

    var gl = canvas.getContext("webgl", { alpha: true, antialias: true, premultipliedAlpha: false });
    if (!gl || !gl.getExtension("OES_standard_derivatives")) {
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
        return;
    }

    var program = gl.createProgram();
    gl.attachShader(program, vertexShader);
    gl.attachShader(program, fragmentShader);
    gl.linkProgram(program);
    if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
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
    var uPointer = gl.getUniformLocation(program, "u_pointer");

    var reducedMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var pointer = { x: 0, y: 0 };
    var target = { x: 0, y: 0 };
    var start = performance.now();
    var frame = null;
    var lost = false;

    function resize() {
        var dpr = Math.min(window.devicePixelRatio || 1, 2);
        var width = Math.floor(canvas.clientWidth * dpr);
        var height = Math.floor(canvas.clientHeight * dpr);

        if (canvas.width !== width || canvas.height !== height) {
            canvas.width = width;
            canvas.height = height;
            gl.viewport(0, 0, width, height);
        }
    }

    function draw(time) {
        resize();

        pointer.x += (target.x - pointer.x) * 0.04;
        pointer.y += (target.y - pointer.y) * 0.04;

        gl.uniform2f(uRes, canvas.width, canvas.height);
        gl.uniform1f(uTime, time);
        gl.uniform2f(uPointer, pointer.x, pointer.y);
        gl.drawArrays(gl.TRIANGLES, 0, 6);
    }

    function loop(now) {
        if (lost) {
            return;
        }
        draw((now - start) / 1000);
        frame = requestAnimationFrame(loop);
    }

    canvas.addEventListener("webglcontextlost", function (e) {
        e.preventDefault();
        lost = true;
        cancelAnimationFrame(frame);
        canvas.classList.remove("is-ready");
    });

    if (reducedMotion) {
        // One still frame, redrawn only when the size changes.
        draw(12);
        window.addEventListener("resize", function () { draw(12); });
    } else {
        window.addEventListener("pointermove", function (e) {
            target.x = (e.clientX / window.innerWidth) * 2 - 1;
            target.y = -((e.clientY / window.innerHeight) * 2 - 1);
        }, { passive: true });

        frame = requestAnimationFrame(loop);
    }

    canvas.classList.add("is-ready");
})();
