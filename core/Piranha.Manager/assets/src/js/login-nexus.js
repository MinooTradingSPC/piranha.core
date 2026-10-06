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
 * Graphic of the "Nexus" login design (login-nexus.scss): the reference's
 * isometric scene, drawn on a 2D canvas with an orthographic projection from
 * the same camera angle. A stepped platform on a fading floor grid carries a
 * cyan core whose glow pulses, two open wire rings turn above it and six
 * wire-caged nodes bob around it, while the whole group sways slowly and
 * drifts a little with the pointer. The script adds its own canvas to the
 * brand column and needs no library. Without canvas support it does nothing
 * and the CSS fallback grid stays. It draws one still frame when reduced
 * motion is preferred and stops drawing while the tab is hidden.
 */
(function () {
    "use strict";

    var host = document.querySelector("#login .login-brand");
    if (!host || host.querySelector(".nexus-scene")) {
        return;
    }

    var canvas = document.createElement("canvas");
    var ctx = canvas.getContext && canvas.getContext("2d");
    if (!ctx) {
        return;
    }
    canvas.className = "nexus-scene";
    canvas.setAttribute("aria-hidden", "true");
    host.insertBefore(canvas, host.firstChild);
    host.classList.add("has-scene");

    // Camera at (20, 20, 20) looking at the origin: screen axes and the
    // direction toward the viewer.
    var S2 = Math.SQRT2;
    var S6 = Math.sqrt(6);
    var S3 = Math.sqrt(3);
    var VIEW = [1 / S3, 1 / S3, 1 / S3];
    var LIGHT = normalize([10, 20, 10]);

    var CYAN = [0, 229, 255];
    var width = 0;
    var height = 0;
    var dpr = 1;
    var scale = 1;
    var cx = 0;
    var cy = 0;

    function normalize(v) {
        var l = Math.sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
        return [v[0] / l, v[1] / l, v[2] / l];
    }

    function dot(a, b) {
        return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
    }

    function rotX(v, a) {
        var c = Math.cos(a), s = Math.sin(a);
        return [v[0], v[1] * c - v[2] * s, v[1] * s + v[2] * c];
    }

    function rotY(v, a) {
        var c = Math.cos(a), s = Math.sin(a);
        return [v[0] * c + v[2] * s, v[1], -v[0] * s + v[2] * c];
    }

    function project(p) {
        return [
            cx + ((p[0] - p[2]) / S2) * scale,
            cy - ((-p[0] + 2 * p[1] - p[2]) / S6) * scale
        ];
    }

    function rgba(c, a) {
        return "rgba(" + Math.round(c[0]) + "," + Math.round(c[1]) + "," + Math.round(c[2]) + "," + a + ")";
    }

    function shade(c, k) {
        return [c[0] * k, c[1] * k, c[2] * k];
    }

    // Box vertices by bit: 1 = +x, 2 = +y, 4 = +z.
    var FACES = [
        { n: [1, 0, 0], v: [1, 3, 7, 5] },
        { n: [-1, 0, 0], v: [0, 4, 6, 2] },
        { n: [0, 1, 0], v: [2, 6, 7, 3] },
        { n: [0, -1, 0], v: [0, 1, 5, 4] },
        { n: [0, 0, 1], v: [4, 5, 7, 6] },
        { n: [0, 0, -1], v: [0, 2, 3, 1] }
    ];
    var EDGES = [[0, 1], [2, 3], [4, 5], [6, 7], [0, 2], [1, 3], [4, 6], [5, 7], [0, 4], [1, 5], [2, 6], [3, 7]];

    // Transforms a box into screen points plus its world-space faces.
    function boxGeometry(box, group) {
        var pts = [];
        for (var i = 0; i < 8; i++) {
            var v = [
                (i & 1 ? .5 : -.5) * box.size[0],
                (i & 2 ? .5 : -.5) * box.size[1],
                (i & 4 ? .5 : -.5) * box.size[2]
            ];
            if (box.rx) {
                v = rotX(v, box.rx);
            }
            if (box.ry) {
                v = rotY(v, box.ry);
            }
            v = rotY([v[0] + box.pos[0], v[1] + box.pos[1], v[2] + box.pos[2]], group);
            pts.push(project(v));
        }
        var faces = FACES.map(function (f) {
            var n = f.n;
            if (box.rx) {
                n = rotX(n, box.rx);
            }
            if (box.ry) {
                n = rotY(n, box.ry);
            }
            n = rotY(n, group);
            return { n: n, v: f.v, visible: dot(n, VIEW) > 1e-4 };
        });
        return { pts: pts, faces: faces };
    }

    function facePath(pts, idx) {
        ctx.beginPath();
        ctx.moveTo(pts[idx[0]][0], pts[idx[0]][1]);
        for (var i = 1; i < idx.length; i++) {
            ctx.lineTo(pts[idx[i]][0], pts[idx[i]][1]);
        }
        ctx.closePath();
    }

    // A lit solid box with outlined visible faces. coreLit tints its top
    // faces with the core's light and bloom gives it a cyan glow.
    function drawBox(box, group, env) {
        var g = boxGeometry(box, group);
        ctx.save();
        if (box.bloom) {
            ctx.shadowColor = rgba(CYAN, .9);
            ctx.shadowBlur = box.bloom * dpr;
        }
        g.faces.forEach(function (f) {
            if (!f.visible) {
                return;
            }
            var k = box.emissive + .8 * Math.max(0, dot(f.n, LIGHT)) + .4 * box.ambient;
            facePath(g.pts, f.v);
            ctx.fillStyle = rgba(shade(box.color, Math.min(k, 1.25)), 1);
            ctx.fill();
        });
        ctx.restore();

        g.faces.forEach(function (f) {
            if (!f.visible) {
                return;
            }
            if (box.coreLit && f.n[1] > .5) {
                // The core's point light on the top faces.
                ctx.save();
                facePath(g.pts, f.v);
                ctx.clip();
                var r = 9 * scale;
                var grad = ctx.createRadialGradient(env.core[0], env.core[1], 0, env.core[0], env.core[1], r);
                grad.addColorStop(0, rgba(CYAN, .22 + .2 * env.pulse));
                grad.addColorStop(.45, rgba(CYAN, .06 + .05 * env.pulse));
                grad.addColorStop(1, rgba(CYAN, 0));
                ctx.fillStyle = grad;
                ctx.fillRect(0, 0, canvas.width, canvas.height);
                ctx.restore();
            }
            if (box.edge) {
                facePath(g.pts, f.v);
                ctx.strokeStyle = box.edge;
                ctx.stroke();
            }
        });
        return g;
    }

    // Wire cage: the edges behind the node first, then the node, then the
    // edges in front.
    function drawNode(node, group, env) {
        var cage = {
            pos: node.pos, size: [.9, .9, .9], rx: node.rx, ry: node.ry
        };
        var g = boxGeometry(cage, group);

        function edges(inFront) {
            ctx.beginPath();
            EDGES.forEach(function (e) {
                // An edge is in front when it borders a visible face.
                var shared = g.faces.some(function (f) {
                    return f.visible && f.v.indexOf(e[0]) !== -1 && f.v.indexOf(e[1]) !== -1;
                });
                if (shared === inFront) {
                    ctx.moveTo(g.pts[e[0]][0], g.pts[e[0]][1]);
                    ctx.lineTo(g.pts[e[1]][0], g.pts[e[1]][1]);
                }
            });
            ctx.strokeStyle = "rgba(255,255,255,.5)";
            ctx.stroke();
        }

        edges(false);
        drawBox({
            pos: node.pos, size: [.6, .6, .6], rx: node.rx, ry: node.ry,
            color: CYAN, emissive: env.emissive, ambient: .4, bloom: 6 + 10 * env.pulse
        }, group, env);
        edges(true);
    }

    // Open cylinder edges: two circles and the segment lines between them.
    function drawRing(ring, group) {
        var seg = 32;
        var top = [];
        var bottom = [];
        for (var i = 0; i < seg; i++) {
            var a = (i / seg) * Math.PI * 2 + ring.rot;
            var x = Math.cos(a) * 4.5;
            var z = Math.sin(a) * 4.5;
            top.push(project(rotY([x, ring.y + .75, z], group)));
            bottom.push(project(rotY([x, ring.y - .75, z], group)));
        }
        ctx.beginPath();
        for (var j = 0; j < seg; j++) {
            var k = (j + 1) % seg;
            ctx.moveTo(top[j][0], top[j][1]);
            ctx.lineTo(top[k][0], top[k][1]);
            ctx.moveTo(bottom[j][0], bottom[j][1]);
            ctx.lineTo(bottom[k][0], bottom[k][1]);
            ctx.moveTo(top[j][0], top[j][1]);
            ctx.lineTo(bottom[j][0], bottom[j][1]);
        }
        ctx.strokeStyle = rgba(CYAN, .3);
        ctx.stroke();
    }

    // Floor grid (30 units, 15 divisions) fading with distance, with faint
    // dots where the lines cross.
    function drawGrid(group) {
        var y = -2;
        var steps = 15;
        for (var i = 0; i <= steps; i++) {
            var u = -15 + i * 2;
            var centre = u === 1 || u === -1;
            for (var s = 0; s < steps; s++) {
                var v0 = -15 + s * 2;
                var v1 = v0 + 2;
                var mid = (v0 + v1) / 2;
                var fade = Math.max(0, 1 - Math.pow(Math.sqrt(u * u + mid * mid) / 15, 2));
                if (fade <= 0) {
                    continue;
                }
                ctx.strokeStyle = centre ? "rgba(51,51,51," + fade + ")" : "rgba(38,38,38," + fade + ")";
                ctx.beginPath();
                var a = project(rotY([u, y, v0], group));
                var b = project(rotY([u, y, v1], group));
                ctx.moveTo(a[0], a[1]);
                ctx.lineTo(b[0], b[1]);
                a = project(rotY([v0, y, u], group));
                b = project(rotY([v1, y, u], group));
                ctx.moveTo(a[0], a[1]);
                ctx.lineTo(b[0], b[1]);
                ctx.stroke();
            }
        }
        for (var gx = -15; gx <= 15; gx += 2) {
            for (var gz = -15; gz <= 15; gz += 2) {
                var f = Math.max(0, 1 - Math.pow(Math.sqrt(gx * gx + gz * gz) / 15, 2));
                if (f > 0) {
                    var p = project(rotY([gx, y, gz], group));
                    ctx.fillStyle = "rgba(255,255,255," + (.16 * f) + ")";
                    ctx.fillRect(p[0] - dpr * .75, p[1] - dpr * .75, dpr * 1.5, dpr * 1.5);
                }
            }
        }
    }

    var DARK = [17, 17, 17];
    var nodes = [];
    for (var n = 0; n < 6; n++) {
        var angle = (n / 6) * Math.PI * 2;
        var radius = 8 + Math.random() * 2;
        nodes.push({
            x: Math.cos(angle) * radius,
            z: Math.sin(angle) * radius,
            y: Math.random() * 4 + 1,
            offset: Math.random() * Math.PI * 2,
            spin: .6 + Math.random() * .3
        });
    }

    var label = host.querySelector(".login-display");
    var lead = host.querySelector(".login-lead");
    var pointer = { x: 0, y: 0 };
    var target = { x: 0, y: 0 };

    function resize() {
        dpr = Math.min(window.devicePixelRatio || 1, 2);
        var w = Math.max(1, Math.floor(canvas.clientWidth * dpr));
        var h = Math.max(1, Math.floor(canvas.clientHeight * dpr));
        if (w !== width || h !== height) {
            width = w;
            height = h;
            canvas.width = w;
            canvas.height = h;
        }
        // The reference frames the scene with an orthographic camera 30
        // units tall. Here the top ring starts where the floating label's
        // line ends and the platform stays clear of the lead at the bottom.
        var hostHeight = host.clientHeight;
        var top = height * .25;
        var bottom = 40 * dpr;
        if (label) {
            var line = parseFloat(window.getComputedStyle(label, "::after").height) || 64;
            top = (label.offsetTop + label.offsetHeight + line) * dpr;
        }
        if (lead && lead.offsetHeight) {
            bottom = (hostHeight - lead.offsetTop + 16) * dpr;
        }
        scale = Math.max(4 * dpr, Math.min(height / 30, width / 25, (height - top - bottom) / 17));
        cx = width / 2;
        cy = top + 9 * scale;
    }

    function draw(time) {
        resize();
        ctx.clearRect(0, 0, width, height);
        ctx.lineWidth = dpr;
        ctx.lineJoin = "round";

        pointer.x += (target.x - pointer.x) * .04;
        pointer.y += (target.y - pointer.y) * .04;

        var group = Math.sin(time * .1) * .1 + pointer.x * .08;
        cy += pointer.y * 6 * dpr;

        var pulse = (Math.sin(time * 3) + 1) * .5;
        var env = {
            pulse: pulse,
            emissive: .3 + pulse * .7,
            core: project(rotY([0, 1.25, 0], group))
        };

        drawGrid(group);

        // Nodes and rings are sorted against the base by depth.
        var items = [];
        nodes.forEach(function (node) {
            var pos = [node.x, node.y + Math.sin(time * 2 + node.offset) * .5, node.z];
            items.push({
                depth: dot(rotY(pos, group), VIEW),
                draw: function () {
                    drawNode({ pos: pos, rx: time * node.spin, ry: time * node.spin }, group, env);
                }
            });
        });
        items.push({ depth: dot([0, 4, 0], VIEW), draw: function () { drawRing({ y: 4, rot: time * .5 }, group); } });
        items.push({ depth: dot([0, 7, 0], VIEW), draw: function () { drawRing({ y: 7, rot: -time * .3 }, group); } });
        items.push({
            depth: 0,
            draw: function () {
                drawBox({ pos: [0, -1, 0], size: [14, 1.5, 14], color: DARK, emissive: 0, ambient: 1, edge: "#333", coreLit: true }, group, env);
                drawBox({ pos: [0, .25, 0], size: [8, 1, 8], color: DARK, emissive: 0, ambient: 1, edge: "#444", coreLit: true }, group, env);
                for (var i = 0; i < 3; i++) {
                    drawBox({ pos: [4.2, .25, -2 + i * 2], size: [.4, .4, 1.5], color: CYAN, emissive: 0, ambient: 1 }, group, env);
                }
                drawBox({ pos: [0, 1.25, 0], size: [3, 1, 3], color: CYAN, emissive: env.emissive * .6, ambient: .4, bloom: 10 + 24 * pulse }, group, env);

                // Soft bloom of the core light.
                var r = 7 * scale;
                var grad = ctx.createRadialGradient(env.core[0], env.core[1], 0, env.core[0], env.core[1], r);
                grad.addColorStop(0, rgba(CYAN, .1 + .1 * pulse));
                grad.addColorStop(1, rgba(CYAN, 0));
                ctx.save();
                ctx.globalCompositeOperation = "lighter";
                ctx.fillStyle = grad;
                ctx.fillRect(env.core[0] - r, env.core[1] - r, r * 2, r * 2);
                ctx.restore();
            }
        });
        items.sort(function (a, b) { return a.depth - b.depth; });
        items.forEach(function (item) { item.draw(); });
    }

    var reducedMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var start = performance.now();
    var frame = null;

    function loop(now) {
        draw((now - start) / 1000);
        frame = requestAnimationFrame(loop);
    }

    function play() {
        if (frame === null && !document.hidden) {
            frame = requestAnimationFrame(loop);
        }
    }

    function pause() {
        if (frame !== null) {
            cancelAnimationFrame(frame);
            frame = null;
        }
    }

    if (reducedMotion) {
        // One still frame, redrawn only when the size changes.
        var still = function () { draw(0); };
        still();
        if (window.ResizeObserver) {
            new ResizeObserver(still).observe(host);
        } else {
            window.addEventListener("resize", still);
        }
    } else {
        window.addEventListener("pointermove", function (e) {
            target.x = (e.clientX / window.innerWidth) * 2 - 1;
            target.y = (e.clientY / window.innerHeight) * 2 - 1;
        }, { passive: true });

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
