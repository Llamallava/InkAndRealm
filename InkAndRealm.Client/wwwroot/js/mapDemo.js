const treeImage = new Image();
let treeImageReady = false;
const buildingImage = new Image();
let buildingImageReady = false;

treeImage.onload = () => {
    treeImageReady = true;
};

treeImage.onerror = () => {
    treeImageReady = false;
};

treeImage.src = "/assets/Summer%20Set/tree_1.png";

buildingImage.onload = () => {
    buildingImageReady = true;
};

buildingImage.onerror = () => {
    buildingImageReady = false;
};

buildingImage.src = "/assets/Summer%20Set/building_2.png";

window.inkAndRealmDemo = {
    drawMap: (canvasId, renderState) => {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            return;
        }

        const ctx = canvas.getContext("2d");
        const getNumber = (value, fallback) => (Number.isFinite(value) ? value : fallback);
        const getFeatureScale = (feature) => {
            const raw = feature && Number.isFinite(feature.size) ? feature.size : 1;
            return raw > 0 ? raw : 1;
        };
        const viewState = renderState && renderState.viewState ? renderState.viewState : null;
        const zoom = viewState ? Math.max(0.25, Math.min(getNumber(viewState.zoom, 1), 4)) : 1;
        const viewX = viewState ? getNumber(viewState.viewX, 0) : 0;
        const viewY = viewState ? getNumber(viewState.viewY, 0) : 0;
        const mapWidth = viewState ? getNumber(viewState.mapWidth, canvas.width) : canvas.width;
        const mapHeight = viewState ? getNumber(viewState.mapHeight, canvas.height) : canvas.height;

        ctx.setTransform(1, 0, 0, 1, 0, 0);
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        ctx.fillStyle = "#e9e2d6";
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        ctx.setTransform(zoom, 0, 0, zoom, -viewX * zoom, -viewY * zoom);

        ctx.fillStyle = "#f5f1e8";
        ctx.fillRect(0, 0, mapWidth, mapHeight);

        ctx.strokeStyle = "#e1ddd4";
        ctx.lineWidth = 1 / zoom;
        for (let x = 0; x <= mapWidth; x += 40) {
            ctx.beginPath();
            ctx.moveTo(x, 0);
            ctx.lineTo(x, mapHeight);
            ctx.stroke();
        }
        for (let y = 0; y <= mapHeight; y += 40) {
            ctx.beginPath();
            ctx.moveTo(0, y);
            ctx.lineTo(mapWidth, y);
            ctx.stroke();
        }

        ctx.strokeStyle = "#c9c2b6";
        ctx.lineWidth = 2 / zoom;
        ctx.strokeRect(0, 0, mapWidth, mapHeight);

        const getLayerColor = (featureType) => {
            switch (featureType) {
                case "Water":
                    return "#7fb7d9";
                case "Tree":
                    return "#9bc97c";
                case "House":
                    return "#d6c2a4";
                case "Land":
                    return "#d9c5a1";
                default:
                    return "#c9d8b6";
            }
        };

        /*
        const drawStroke = (points, radius, color, alpha = 1) => {
            if (!Array.isArray(points) || points.length === 0) {
                return;
            }

            ctx.save();
            ctx.globalAlpha = alpha;
            ctx.lineWidth = radius * 2;
            ctx.lineCap = "round";
            ctx.lineJoin = "round";
            ctx.strokeStyle = color;

            if (points.length === 1) {
                ctx.fillStyle = color;
                ctx.beginPath();
                ctx.arc(points[0].x, points[0].y, radius, 0, Math.PI * 2);
                ctx.fill();
                ctx.restore();
                return;
            }

            ctx.beginPath();
            ctx.moveTo(points[0].x, points[0].y);
            for (let i = 1; i < points.length; i += 1) {
                ctx.lineTo(points[i].x, points[i].y);
            }
            ctx.stroke();
            ctx.restore();
        };
        */

        const drawSmoothPolygon = (targetCtx, points, color, alpha = 1, strokeColor = null) => {
            if (!Array.isArray(points) || points.length < 3) {
                return;
            }

            const getMidpoint = (left, right) => ({
                x: (left.x + right.x) / 2,
                y: (left.y + right.y) / 2
            });

            targetCtx.save();
            targetCtx.globalAlpha = alpha;
            targetCtx.fillStyle = color;
            if (strokeColor) {
                targetCtx.strokeStyle = strokeColor;
                targetCtx.lineWidth = 1 / zoom;
            }

            const last = points[points.length - 1];
            const first = points[0];
            const start = getMidpoint(last, first);

            targetCtx.beginPath();
            targetCtx.moveTo(start.x, start.y);

            for (let i = 0; i < points.length; i += 1) {
                const current = points[i];
                const next = points[(i + 1) % points.length];
                const mid = getMidpoint(current, next);
                targetCtx.quadraticCurveTo(current.x, current.y, mid.x, mid.y);
            }

            targetCtx.closePath();
            targetCtx.fill();
            if (strokeColor) {
                targetCtx.stroke();
            }
            targetCtx.restore();
        };

        const drawPolygon = (targetCtx, points, color, alpha = 1, strokeColor = null) => {
            if (!Array.isArray(points) || points.length < 3) {
                return;
            }

            targetCtx.save();
            targetCtx.globalAlpha = alpha;
            targetCtx.fillStyle = color;
            if (strokeColor) {
                targetCtx.strokeStyle = strokeColor;
                targetCtx.lineWidth = 1 / zoom;
            }

            targetCtx.beginPath();
            targetCtx.moveTo(points[0].x, points[0].y);
            for (let i = 1; i < points.length; i += 1) {
                targetCtx.lineTo(points[i].x, points[i].y);
            }
            targetCtx.closePath();
            targetCtx.fill();
            if (strokeColor) {
                targetCtx.stroke();
            }
            targetCtx.restore();
        };

        const hashValue = (value) => {
            const raw = Math.sin(value) * 10000;
            return raw - Math.floor(raw);
        };

        const buildChaoticPoints = (points, segmentLength, amplitude, seed) => {
            if (!Array.isArray(points) || points.length < 2) {
                return points || [];
            }

            const jittered = [];
            for (let i = 0; i < points.length; i += 1) {
                const current = points[i];
                const next = points[(i + 1) % points.length];
                const dx = next.x - current.x;
                const dy = next.y - current.y;
                const length = Math.hypot(dx, dy);
                if (!Number.isFinite(length) || length <= 0.001) {
                    jittered.push({ x: current.x, y: current.y });
                    continue;
                }

                const steps = Math.max(1, Math.floor(length / segmentLength));
                const nx = -dy / length;
                const ny = dx / length;
                jittered.push({ x: current.x, y: current.y });

                for (let step = 1; step < steps; step += 1) {
                    const t = step / steps;
                    const baseX = current.x + dx * t;
                    const baseY = current.y + dy * t;
                    const rand = hashValue(seed + (i * 127.1) + (step * 311.7));
                    const offset = (rand * 2 - 1) * amplitude;
                    jittered.push({
                        x: baseX + (nx * offset),
                        y: baseY + (ny * offset)
                    });
                }
            }

            return jittered;
        };

        const drawChaoticPolygon = (targetCtx, points, color, alpha = 1, strokeColor = null) => {
            if (!Array.isArray(points) || points.length < 3) {
                return;
            }

            const seed = points.reduce((total, point) => total + (point.x * 0.13) + (point.y * 0.71), 0);
            const chaoticPoints = buildChaoticPoints(points, 28, 7, seed);
            drawPolygon(targetCtx, chaoticPoints, color, alpha, strokeColor);
        };

        const drawLandPolygon = (targetCtx, points, color, alpha = 1, strokeColor = null) => {
            if (chaoticLandEdges) {
                drawChaoticPolygon(targetCtx, points, color, alpha, strokeColor);
            } else {
                drawPolygon(targetCtx, points, color, alpha, strokeColor);
            }
        };

        const drawPolygonHandles = (targetCtx, points, selectedIndex = null) => {
            if (!Array.isArray(points) || points.length === 0) {
                return;
            }

            const handleRadius = 5 / zoom;
            points.forEach((point, index) => {
                targetCtx.beginPath();
                targetCtx.fillStyle = index === selectedIndex ? "#f6c343" : "#f7f4ef";
                targetCtx.strokeStyle = "#2b3a4a";
                targetCtx.lineWidth = 1 / zoom;
                targetCtx.arc(point.x, point.y, handleRadius, 0, Math.PI * 2);
                targetCtx.fill();
                targetCtx.stroke();
            });
        };

        const drawPolygonEdgeHandles = (targetCtx, points, activeEdgeIndex = null) => {
            if (!Array.isArray(points) || points.length < 2) {
                return;
            }

            const handleRadius = 4 / zoom;
            points.forEach((point, index) => {
                const next = points[(index + 1) % points.length];
                const midX = (point.x + next.x) / 2;
                const midY = (point.y + next.y) / 2;
                targetCtx.beginPath();
                targetCtx.fillStyle = index === activeEdgeIndex ? "#ffd28a" : "#d8e4ef";
                targetCtx.strokeStyle = "#2b3a4a";
                targetCtx.lineWidth = 1 / zoom;
                targetCtx.arc(midX, midY, handleRadius, 0, Math.PI * 2);
                targetCtx.fill();
                targetCtx.stroke();
            });
        };

        const drawPointEditHandle = (targetCtx, feature) => {
            if (!feature || !Number.isFinite(feature.x) || !Number.isFinite(feature.y)) {
                return;
            }

            const handleRadius = 10 / zoom;
            targetCtx.save();
            targetCtx.fillStyle = "rgba(111, 174, 211, 0.2)";
            targetCtx.strokeStyle = "#2f5d89";
            targetCtx.lineWidth = 2 / zoom;
            targetCtx.beginPath();
            targetCtx.arc(feature.x, feature.y, handleRadius, 0, Math.PI * 2);
            targetCtx.fill();
            targetCtx.stroke();
            targetCtx.restore();
        };

        const drawTitles = (titles) => {
            if (!Array.isArray(titles) || titles.length === 0) {
                return;
            }

            ctx.save();
            ctx.setTransform(1, 0, 0, 1, 0, 0);
            ctx.textAlign = "center";
            ctx.textBaseline = "bottom";
            ctx.font = "16px 'Segoe UI', sans-serif";

            titles.forEach(title => {
                if (!title || !title.name) {
                    return;
                }

                const screenX = (title.x - viewX) * zoom;
                const screenY = (title.y - viewY) * zoom;
                if (screenX < -50 || screenX > canvas.width + 50 || screenY < -50 || screenY > canvas.height + 50) {
                    return;
                }

                ctx.lineWidth = 3;
                ctx.strokeStyle = "rgba(255, 255, 255, 0.8)";
                ctx.fillStyle = title.isStaged ? "rgba(45, 58, 74, 0.8)" : "#2b3a4a";
                ctx.strokeText(title.name, screenX, screenY);
                ctx.fillText(title.name, screenX, screenY);
            });

            ctx.restore();
        };

        const drawBrushCursor = (targetCtx, brushPreview) => {
            if (!brushPreview || !Number.isFinite(brushPreview.x) || !Number.isFinite(brushPreview.y)) {
                return;
            }

            const radius = Number.isFinite(brushPreview.radius) ? brushPreview.radius : 0;
            if (radius <= 0) {
                return;
            }

            targetCtx.save();
            targetCtx.globalAlpha = brushPreview.isActive ? 0.35 : 0.25;
            targetCtx.fillStyle = "rgba(111, 174, 211, 0.2)";
            targetCtx.strokeStyle = "#2f5d89";
            targetCtx.lineWidth = 2 / zoom;
            targetCtx.setLineDash([5 / zoom, 4 / zoom]);
            targetCtx.beginPath();
            targetCtx.arc(brushPreview.x, brushPreview.y, radius, 0, Math.PI * 2);
            targetCtx.fill();
            targetCtx.stroke();
            targetCtx.restore();
        };

        const drawTree = (x, y, canopyColor, trunkColor, outlineColor) => {
            ctx.fillStyle = trunkColor;
            ctx.fillRect(x - 3, y + 6, 6, 10);

            ctx.beginPath();
            ctx.fillStyle = canopyColor;
            ctx.arc(x, y, 10, 0, Math.PI * 2);
            ctx.fill();

            if (outlineColor) {
                ctx.strokeStyle = outlineColor;
                ctx.beginPath();
                ctx.arc(x, y, 12, 0, Math.PI * 2);
                ctx.stroke();
            }
        };

        const drawPalm = (x, y, canopyColor, trunkColor, outlineColor) => {
            ctx.fillStyle = trunkColor;
            ctx.fillRect(x - 2, y + 2, 4, 16);

            ctx.strokeStyle = canopyColor;
            ctx.lineWidth = 3;
            ctx.lineCap = "round";
            ctx.beginPath();
            ctx.moveTo(x, y + 2);
            ctx.lineTo(x - 12, y - 6);
            ctx.moveTo(x, y + 2);
            ctx.lineTo(x - 4, y - 10);
            ctx.moveTo(x, y + 2);
            ctx.lineTo(x + 4, y - 10);
            ctx.moveTo(x, y + 2);
            ctx.lineTo(x + 12, y - 6);
            ctx.stroke();

            if (outlineColor) {
                ctx.strokeStyle = outlineColor;
                ctx.lineWidth = 1;
                ctx.beginPath();
                ctx.arc(x, y - 2, 12, 0, Math.PI * 2);
                ctx.stroke();
            }
        };

        const drawTilesetTree = (x, y, isStaged) => {
            if (!treeImageReady) {
                return false;
            }

            const targetHeight = 48;
            const scale = targetHeight / treeImage.naturalHeight;
            const targetWidth = treeImage.naturalWidth * scale;

            ctx.save();
            ctx.globalAlpha = isStaged ? 0.75 : 1;
            ctx.imageSmoothingEnabled = true;
            ctx.drawImage(
                treeImage,
                x - targetWidth * 0.5,
                y - targetHeight,
                targetWidth,
                targetHeight
            );
            ctx.restore();

            return true;
        };

        const treeStylePalette = {
            Oak: { canopy: "#4a8f5a", trunk: "#5c4b32", outline: null },
            Pine: { canopy: "#3b7a4a", trunk: "#4c3c2a", outline: null },
            Birch: { canopy: "#6aa84f", trunk: "#c9c0b0", outline: null },
            Palm: { canopy: "#6f9f4a", trunk: "#7a5a3a", outline: null }
        };

        const getTreePalette = (styleKey, isStaged) => {
            const base = treeStylePalette[styleKey] || treeStylePalette.Oak;
            if (!isStaged) {
                return base;
            }

            return {
                canopy: "#7bb661",
                trunk: "#6b5436",
                outline: "#2f5d39"
            };
        };

        const drawHouse = (x, y, baseColor, roofColor, outlineColor) => {
            if (buildingImageReady) {
                const targetHeight = 96;
                const scale = targetHeight / buildingImage.naturalHeight;
                const targetWidth = buildingImage.naturalWidth * scale;

                ctx.save();
                ctx.globalAlpha = 1;
                ctx.imageSmoothingEnabled = true;
                ctx.drawImage(
                    buildingImage,
                    x - targetWidth * 0.5,
                    y - targetHeight,
                    targetWidth,
                    targetHeight
                );
                ctx.restore();
                return;
            }

            ctx.fillStyle = baseColor;
            ctx.fillRect(x - 10, y - 2, 20, 14);

            ctx.beginPath();
            ctx.fillStyle = roofColor;
            ctx.moveTo(x - 12, y - 2);
            ctx.lineTo(x, y - 14);
            ctx.lineTo(x + 12, y - 2);
            ctx.closePath();
            ctx.fill();

            if (outlineColor) {
                ctx.strokeStyle = outlineColor;
                ctx.strokeRect(x - 12, y - 16, 24, 28);
            }
        };

        const drawWithScale = (feature, draw) => {
            const scale = getFeatureScale(feature);
            if (scale === 1) {
                draw(feature.x, feature.y);
                return;
            }

            ctx.save();
            ctx.translate(feature.x, feature.y);
            ctx.scale(scale, scale);
            draw(0, 0);
            ctx.restore();
        };

        const drawTreeAt = (x, y, styleKey, isStaged) => {
            const palette = getTreePalette(styleKey, isStaged);
            if (drawTilesetTree(x, y, isStaged)) {
                return;
            }

            if (styleKey === "Palm") {
                drawPalm(x, y, palette.canopy, palette.trunk, palette.outline);
            } else {
                drawTree(x, y, palette.canopy, palette.trunk, palette.outline);
            }
        };

        const drawHouseAt = (x, y, isStaged) => {
            drawHouse(
                x,
                y,
                isStaged ? "#e3c9a8" : "#d7b894",
                isStaged ? "#9a6a42" : "#7f5a3b",
                isStaged ? "#6a4a2d" : null
            );
        };

        const pointRenderers = {
            Tree: (feature) => {
                const isStaged = !!feature.isStaged;
                drawWithScale(feature, (x, y) => drawTreeAt(x, y, feature.styleKey, isStaged));
            },
            House: (feature) => {
                const isStaged = !!feature.isStaged;
                drawWithScale(feature, (x, y) => drawHouseAt(x, y, isStaged));
            }
        };

        /*
        const layerCanvases = new Map();
        if (renderState && Array.isArray(renderState.areaLayers)) {
            const layers = renderState.areaLayers
                .slice()
                .sort((left, right) => (left.layerIndex ?? 0) - (right.layerIndex ?? 0));
            layers.forEach(layer => {
                if (!Array.isArray(layer.strokes)) {
                    return;
                }

                const color = getLayerColor(layer.featureType);
                const layerCanvas = document.createElement("canvas");
                layerCanvas.width = mapWidth;
                layerCanvas.height = mapHeight;
                const layerCtx = layerCanvas.getContext("2d");
                if (!layerCtx) {
                    return;
                }

                layerCtx.clearRect(0, 0, mapWidth, mapHeight);
                layer.strokes.forEach(stroke => {
                    const radius = stroke.radius && stroke.radius > 0 ? stroke.radius : 18;
                    if (!Array.isArray(stroke.points) || stroke.points.length === 0) {
                        return;
                    }

                    layerCtx.save();
                    layerCtx.lineWidth = radius * 2;
                    layerCtx.lineCap = "round";
                    layerCtx.lineJoin = "round";
                    layerCtx.strokeStyle = color;
                    layerCtx.globalAlpha = 1;

                    if (stroke.points.length === 1) {
                        layerCtx.fillStyle = color;
                        layerCtx.beginPath();
                        layerCtx.arc(stroke.points[0].x, stroke.points[0].y, radius, 0, Math.PI * 2);
                        layerCtx.fill();
                    } else {
                        layerCtx.beginPath();
                        layerCtx.moveTo(stroke.points[0].x, stroke.points[0].y);
                        for (let i = 1; i < stroke.points.length; i += 1) {
                            layerCtx.lineTo(stroke.points[i].x, stroke.points[i].y);
                        }
                        layerCtx.stroke();
                    }
                    layerCtx.restore();
                });

                ctx.save();
                ctx.globalAlpha = 0.85;
                ctx.drawImage(layerCanvas, 0, 0);
                ctx.restore();

                const layerIndex = Number.isFinite(layer.layerIndex) ? layer.layerIndex : 0;
                layerCanvases.set(layerIndex, layerCanvas);
            });
        }
        */

        const chaoticLandEdges = !!(renderState && renderState.useChaoticLandEdges);
        const layerCanvases = new Map();
        if (renderState && Array.isArray(renderState.areaPolygons)) {
            const polygonsByLayer = new Map();
            renderState.areaPolygons.forEach(polygon => {
                if (!polygon || !Array.isArray(polygon.points) || polygon.points.length < 3) {
                    return;
                }

                const layerIndex = Number.isFinite(polygon.layerIndex) ? polygon.layerIndex : 0;
                if (!polygonsByLayer.has(layerIndex)) {
                    polygonsByLayer.set(layerIndex, []);
                }
                polygonsByLayer.get(layerIndex).push(polygon);
            });

            Array.from(polygonsByLayer.entries())
                .sort((left, right) => left[0] - right[0])
                .forEach(([layerIndex, polygons]) => {
                    const layerCanvas = document.createElement("canvas");
                    layerCanvas.width = mapWidth;
                    layerCanvas.height = mapHeight;
                    const layerCtx = layerCanvas.getContext("2d");
                    if (!layerCtx) {
                        return;
                    }

                    layerCtx.clearRect(0, 0, mapWidth, mapHeight);
                    polygons.forEach(polygon => {
                        const color = getLayerColor(polygon.featureType);
                        if (polygon.featureType === "Water") {
                            drawSmoothPolygon(layerCtx, polygon.points, color, 0.85, "#5a86a1");
                        } else if (polygon.featureType === "Land") {
                            drawLandPolygon(layerCtx, polygon.points, color, 0.85, "#8a6a4d");
                        } else {
                            drawPolygon(layerCtx, polygon.points, color, 0.85, "#7b6b4a");
                        }
                    });

                    ctx.save();
                    ctx.globalAlpha = 0.9;
                    ctx.drawImage(layerCanvas, 0, 0);
                    ctx.restore();

                    layerCanvases.set(layerIndex, layerCanvas);
                });
        }

        /*
        if (renderState && Array.isArray(renderState.activeStrokes)) {
            const activeByLayer = new Map();
            renderState.activeStrokes.forEach(stroke => {
                if (!stroke || !Array.isArray(stroke.points) || stroke.points.length === 0) {
                    return;
                }

                const layerIndex = Number.isFinite(stroke.layerIndex) ? stroke.layerIndex : 0;
                if (!activeByLayer.has(layerIndex)) {
                    activeByLayer.set(layerIndex, []);
                }
                activeByLayer.get(layerIndex).push(stroke);
            });

            activeByLayer.forEach((strokes, layerIndex) => {
                const previewCanvas = document.createElement("canvas");
                previewCanvas.width = mapWidth;
                previewCanvas.height = mapHeight;
                const previewCtx = previewCanvas.getContext("2d");
                if (!previewCtx) {
                    return;
                }

                strokes.forEach(stroke => {
                    const radius = stroke.radius && stroke.radius > 0 ? stroke.radius : 18;
                    if (!Array.isArray(stroke.points) || stroke.points.length === 0) {
                        return;
                    }

                    previewCtx.save();
                    previewCtx.lineWidth = radius * 2;
                    previewCtx.lineCap = "round";
                    previewCtx.lineJoin = "round";
                    previewCtx.strokeStyle = "#7fb7d9";
                    previewCtx.globalAlpha = 1;

                    if (stroke.points.length === 1) {
                        previewCtx.fillStyle = "#7fb7d9";
                        previewCtx.beginPath();
                        previewCtx.arc(stroke.points[0].x, stroke.points[0].y, radius, 0, Math.PI * 2);
                        previewCtx.fill();
                    } else {
                        previewCtx.beginPath();
                        previewCtx.moveTo(stroke.points[0].x, stroke.points[0].y);
                        for (let i = 1; i < stroke.points.length; i += 1) {
                            previewCtx.lineTo(stroke.points[i].x, stroke.points[i].y);
                        }
                        previewCtx.stroke();
                    }
                    previewCtx.restore();
                });

                const existingLayer = layerCanvases.get(layerIndex);
                if (existingLayer) {
                    previewCtx.save();
                    previewCtx.globalCompositeOperation = "destination-out";
                    previewCtx.drawImage(existingLayer, 0, 0);
                    previewCtx.restore();
                }

                ctx.save();
                ctx.globalAlpha = 0.6;
                ctx.drawImage(previewCanvas, 0, 0);
                ctx.restore();
            });
        } else if (renderState && renderState.activeStroke && Array.isArray(renderState.activeStroke.points)) {
            const radius = renderState.activeStroke.radius && renderState.activeStroke.radius > 0
                ? renderState.activeStroke.radius
                : 18;
            drawStroke(renderState.activeStroke.points, radius, "#7fb7d9", 0.6);
        }
        */

        if (renderState && Array.isArray(renderState.activePolygons) && renderState.activePolygons.length > 0) {
            const activeByLayer = new Map();
            renderState.activePolygons.forEach(polygon => {
                if (!polygon || !Array.isArray(polygon.points) || polygon.points.length < 2) {
                    return;
                }

                const layerIndex = Number.isFinite(polygon.layerIndex) ? polygon.layerIndex : 0;
                if (!activeByLayer.has(layerIndex)) {
                    activeByLayer.set(layerIndex, []);
                }
                activeByLayer.get(layerIndex).push(polygon);
            });

            activeByLayer.forEach((polygons, layerIndex) => {
                const previewCanvas = document.createElement("canvas");
                previewCanvas.width = mapWidth;
                previewCanvas.height = mapHeight;
                const previewCtx = previewCanvas.getContext("2d");
                if (!previewCtx) {
                    return;
                }

                polygons.forEach(polygon => {
                    if (polygon.points.length < 2) {
                        return;
                    }

                    if (polygon.points.length >= 3) {
                        if (polygon.featureType === "Water") {
                            drawSmoothPolygon(previewCtx, polygon.points, "#7fb7d9", 0.25, "#5a86a1");
                        } else if (polygon.featureType === "Land") {
                            drawLandPolygon(previewCtx, polygon.points, "#d9c5a1", 0.2, "#8a6a4d");
                        }
                    }

                    previewCtx.save();
                    previewCtx.globalAlpha = 0.55;
                    previewCtx.strokeStyle = polygon.featureType === "Land" ? "#8a6a4d" : "#5a86a1";
                    previewCtx.lineWidth = 2 / zoom;
                    previewCtx.setLineDash([6 / zoom, 4 / zoom]);
                    previewCtx.beginPath();
                    previewCtx.moveTo(polygon.points[0].x, polygon.points[0].y);
                    for (let i = 1; i < polygon.points.length; i += 1) {
                        previewCtx.lineTo(polygon.points[i].x, polygon.points[i].y);
                    }
                    previewCtx.stroke();
                    previewCtx.restore();
                });

                const existingLayer = layerCanvases.get(layerIndex);
                if (existingLayer) {
                    previewCtx.save();
                    previewCtx.globalCompositeOperation = "destination-out";
                    previewCtx.drawImage(existingLayer, 0, 0);
                    previewCtx.restore();
                }

                ctx.save();
                ctx.globalAlpha = 0.75;
                ctx.drawImage(previewCanvas, 0, 0);
                ctx.restore();
            });
        } else if (renderState && renderState.activePolygon && Array.isArray(renderState.activePolygon.points)) {
            const preview = renderState.activePolygon;
            if (preview.points.length === 1) {
                ctx.save();
                ctx.globalAlpha = 0.6;
                ctx.fillStyle = "#5a86a1";
                ctx.beginPath();
                ctx.arc(preview.points[0].x, preview.points[0].y, 4 / zoom, 0, Math.PI * 2);
                ctx.fill();
                ctx.restore();
            }

            if (preview.points.length >= 3) {
                if (preview.featureType === "Water") {
                    drawSmoothPolygon(ctx, preview.points, "#7fb7d9", 0.25, "#5a86a1");
                } else if (preview.featureType === "Land") {
                    drawLandPolygon(ctx, preview.points, "#d9c5a1", 0.2, "#8a6a4d");
                }
            }

            if (preview.points.length >= 2) {
                ctx.save();
                ctx.globalAlpha = 0.55;
                ctx.strokeStyle = preview.featureType === "Land" ? "#8a6a4d" : "#5a86a1";
                ctx.lineWidth = 2 / zoom;
                ctx.setLineDash([6 / zoom, 4 / zoom]);
                ctx.beginPath();
                ctx.moveTo(preview.points[0].x, preview.points[0].y);
                for (let i = 1; i < preview.points.length; i += 1) {
                    ctx.lineTo(preview.points[i].x, preview.points[i].y);
                }
                ctx.stroke();
                ctx.restore();
            }
        }

        if (renderState && renderState.editPolygon && Array.isArray(renderState.editPolygon.points)) {
            const editPolygon = renderState.editPolygon;
            const selectedIndex = Number.isFinite(renderState.editPolygonPointIndex)
                ? renderState.editPolygonPointIndex
                : null;
            const activeEdgeIndex = Number.isFinite(renderState.editPolygonEdgeIndex)
                ? renderState.editPolygonEdgeIndex
                : null;

            if (editPolygon.points.length >= 3) {
                if (editPolygon.featureType === "Water") {
                    drawSmoothPolygon(ctx, editPolygon.points, "#6faed3", 0.18, "#2f5d89");
                } else if (editPolygon.featureType === "Land") {
                    drawLandPolygon(ctx, editPolygon.points, "#d9c5a1", 0.16, "#8a6a4d");
                } else {
                    drawPolygon(ctx, editPolygon.points, "#d9c5a1", 0.16, "#7b6b4a");
                }
            }

            if (editPolygon.points.length >= 2) {
                ctx.save();
                ctx.globalAlpha = 0.7;
                ctx.strokeStyle = editPolygon.featureType === "Land" ? "#8a6a4d" : "#2f5d89";
                ctx.lineWidth = 2 / zoom;
                ctx.setLineDash([4 / zoom, 3 / zoom]);
                ctx.beginPath();
                ctx.moveTo(editPolygon.points[0].x, editPolygon.points[0].y);
                for (let i = 1; i < editPolygon.points.length; i += 1) {
                    ctx.lineTo(editPolygon.points[i].x, editPolygon.points[i].y);
                }
                ctx.closePath();
                ctx.stroke();
                ctx.restore();
            }

            drawPolygonHandles(ctx, editPolygon.points, selectedIndex);
            drawPolygonEdgeHandles(ctx, editPolygon.points, activeEdgeIndex);
        }

        if (renderState && Array.isArray(renderState.pointFeatures)) {
            renderState.pointFeatures.forEach(feature => {
                if (!feature || !feature.featureType) {
                    return;
                }

                const renderer = pointRenderers[feature.featureType];
                if (renderer) {
                    renderer(feature);
                }
            });
        }

        if (renderState && Array.isArray(renderState.titleFeatures)) {
            drawTitles(renderState.titleFeatures);
        }

        if (renderState && renderState.editPointFeature) {
            drawPointEditHandle(ctx, renderState.editPointFeature);
        }

        if (renderState && renderState.brushPreview) {
            drawBrushCursor(ctx, renderState.brushPreview);
        }

    },
    getCanvasClientSize: (canvasId) => {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            return null;
        }

        const rect = canvas.getBoundingClientRect();
        return {
            width: rect.width,
            height: rect.height
        };
    }
};

