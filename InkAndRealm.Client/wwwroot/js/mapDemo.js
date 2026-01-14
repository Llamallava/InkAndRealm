window.inkAndRealmDemo = {
    drawMap: (canvasId, trees, houses, pendingX, pendingY, hasPending, pendingType) => {
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

        const drawTree = (x, y, color) => {
            ctx.fillStyle = "#5c4b32";
            ctx.fillRect(x - 3, y + 6, 6, 10);

            ctx.beginPath();
            ctx.fillStyle = color;
            ctx.arc(x, y, 10, 0, Math.PI * 2);
            ctx.fill();
        };

        const drawHouse = (x, y, baseColor, roofColor) => {
            ctx.fillStyle = baseColor;
            ctx.fillRect(x - 10, y - 2, 20, 14);

            ctx.beginPath();
            ctx.fillStyle = roofColor;
            ctx.moveTo(x - 12, y - 2);
            ctx.lineTo(x, y - 14);
            ctx.lineTo(x + 12, y - 2);
            ctx.closePath();
            ctx.fill();
        };

        if (Array.isArray(trees)) {
            trees.forEach(tree => drawTree(tree.x, tree.y, "#4a8f5a"));
        }

        if (Array.isArray(houses)) {
            houses.forEach(house => drawHouse(house.x, house.y, "#d7b894", "#7f5a3b"));
        }

        if (hasPending) {
            if (pendingType === "House") {
                drawHouse(pendingX, pendingY, "#e3c9a8", "#9a6a42");
                ctx.strokeStyle = "#6a4a2d";
                ctx.strokeRect(pendingX - 14, pendingY - 18, 28, 26);
            } else {
                drawTree(pendingX, pendingY, "#7bb661");
                ctx.strokeStyle = "#2f5d39";
                ctx.beginPath();
                ctx.arc(pendingX, pendingY, 14, 0, Math.PI * 2);
                ctx.stroke();
            }
        }
    }
};
