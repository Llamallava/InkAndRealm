window.inkAndRealmDemo = {
    drawMap: (canvasId, trees, houses, stagedTrees, stagedHouses, areaLayers, activeStroke) => {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            return;
        }

        const ctx = canvas.getContext("2d");
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        ctx.fillStyle = "#f5f1e8";
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        ctx.strokeStyle = "#e1ddd4";
        ctx.lineWidth = 1;
        for (let x = 0; x <= canvas.width; x += 40) {
            ctx.beginPath();
            ctx.moveTo(x, 0);
            ctx.lineTo(x, canvas.height);
            ctx.stroke();
        }
        for (let y = 0; y <= canvas.height; y += 40) {
            ctx.beginPath();
            ctx.moveTo(0, y);
            ctx.lineTo(canvas.width, y);
            ctx.stroke();
        }

        const getLayerColor = (featureType) => {
            switch (featureType) {
                case "Water":
                    return "#7fb7d9";
                case "Land":
                    return "#9bc97c";
                default:
                    return "#c9d8b6";
            }
        };

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

        if (Array.isArray(areaLayers)) {
            areaLayers.forEach(layer => {
                if (!Array.isArray(layer.strokes)) {
                    return;
                }

                const color = getLayerColor(layer.featureType);
                layer.strokes.forEach(stroke => {
                    drawStroke(stroke.points, stroke.radius, color, 0.85);
                });
            });
        }

        if (activeStroke && Array.isArray(activeStroke.points)) {
            drawStroke(activeStroke.points, activeStroke.radius, "#7fb7d9", 0.6);
        }

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

        const drawHouse = (x, y, baseColor, roofColor, outlineColor) => {
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

        if (Array.isArray(trees)) {
            trees.forEach(tree => drawTree(tree.x, tree.y, "#4a8f5a", "#5c4b32"));
        }

        if (Array.isArray(houses)) {
            houses.forEach(house => drawHouse(house.x, house.y, "#d7b894", "#7f5a3b"));
        }

        if (Array.isArray(stagedTrees)) {
            stagedTrees.forEach(tree => drawTree(tree.x, tree.y, "#7bb661", "#6b5436", "#2f5d39"));
        }

        if (Array.isArray(stagedHouses)) {
            stagedHouses.forEach(house => drawHouse(house.x, house.y, "#e3c9a8", "#9a6a42", "#6a4a2d"));
        }
    }
};
