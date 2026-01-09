window.inkAndRealmDemo = {
    drawTrees: (canvasId, trees, pendingX, pendingY, hasPending) => {
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

        if (Array.isArray(trees)) {
            trees.forEach(tree => drawTree(tree.x, tree.y, "#4a8f5a"));
        }

        if (hasPending) {
            drawTree(pendingX, pendingY, "#7bb661");
            ctx.strokeStyle = "#2f5d39";
            ctx.beginPath();
            ctx.arc(pendingX, pendingY, 14, 0, Math.PI * 2);
            ctx.stroke();
        }
    }
};
