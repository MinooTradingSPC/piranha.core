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
 * Login page background: a full-bleed WebGL field drawn by one fragment
 * shader - a perspective floor grid, a fine line lattice, sparse glowing
 * anchors and slow line trails, breathing gently and drifting with the
 * pointer. If WebGL isn't available the canvas stays hidden and the CSS
 * lattice on #login shows through instead.
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

        "float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }",

        // Anti-aliased distance to the nearest integer grid line.
        "float grid(vec2 p) {",
        "    vec2 d = abs(fract(p) - 0.5);",
        "    vec2 w = fwidth(p) * 1.2;",
        "    vec2 l = smoothstep(0.5 - w, vec2(0.5), d);",
        "    return max(l.x, l.y);",
        "}",

        // Brand gradient: lime -> emerald -> blue.
        "vec3 brand(float t) {",
        "    vec3 lime = vec3(0.745, 0.949, 0.392);",
        "    vec3 emerald = vec3(0.204, 0.827, 0.600);",
        "    vec3 blue = vec3(0.376, 0.647, 0.980);",
        "    t = clamp(t, 0.0, 1.0);",
        "    return t < 0.5 ? mix(lime, emerald, t * 2.0) : mix(emerald, blue, t * 2.0 - 1.0);",
        "}",

        "void main() {",
        "    vec2 uv = (gl_FragCoord.xy - 0.5 * u_res) / u_res.y;",
        "    float t = u_time;",
        "    float breath = 0.5 + 0.5 * sin(t * 0.35);",
        "    vec2 drift = u_pointer * 0.035;",
        "    float tint = 0.5 + 0.45 * (uv.x - uv.y);",
        "    vec3 col = vec3(0.0);",

        // Fine lattice across the whole field, with slight parallax.
        "    vec2 lp = (uv + drift) * 16.0;",
        "    col += vec3(0.612, 0.639, 0.686) * grid(lp) * (0.05 + 0.025 * breath);",

        // Sparse anchors at lattice intersections.
        "    vec2 cell = floor(lp + 0.5);",
        "    float h = hash(cell);",
        "    if (h > 0.94) {",
        "        float pulse = 0.5 + 0.5 * sin(t * 0.8 + h * 40.0);",
        "        float d = length(lp - cell);",
        "        col += brand(tint) * exp(-d * 14.0) * (0.35 + 0.65 * pulse) * 0.9;",
        "    }",

        // Line trails travelling along a few horizontal lattice lines.
        // Derivatives are taken outside any branch, since they're
        // undefined in per-pixel control flow.
        "    float row = floor(lp.y + 0.5);",
        "    float hr = hash(vec2(row, 7.0));",
        "    float onLine = 1.0 - smoothstep(0.0, fwidth(lp.y) * 1.5, abs(lp.y - row));",
        "    float s = fract(lp.x * 0.03 - t * (0.02 + 0.03 * hr) + hr * 13.0);",
        "    float trail = smoothstep(0.75, 1.0, s) * (1.0 - smoothstep(0.995, 1.0, s));",
        "    col += brand(tint) * onLine * trail * step(0.82, hr) * 0.8;",

        // Perspective floor grid below the horizon.
        "    float y = 0.08 - uv.y - drift.y * 0.5;",
        "    float z = 0.3 / max(y, 0.01);",
        "    vec2 fp = vec2((uv.x + drift.x) * z * 2.4, z + t * 0.12);",
        "    float fade = smoothstep(0.02, 0.3, y) * exp(-z * 0.06);",
        "    col += mix(vec3(0.82), brand(tint), 0.55) * grid(fp) * fade * (0.22 + 0.1 * breath);",

        // Soft shader gradient glow and vignette.
        "    float glow = exp(-length(uv - vec2(0.35, 0.25) - drift) * 2.2);",
        "    col += brand(tint) * glow * (0.05 + 0.04 * breath);",
        "    col *= 1.0 - 0.55 * smoothstep(0.35, 1.1, length(uv));",

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
