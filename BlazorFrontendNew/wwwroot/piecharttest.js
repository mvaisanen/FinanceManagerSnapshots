function pie(id, dataPoints) {
    console.debug("Pie() jonka id="+id+" kutsuttu datalla:");
    //console.log(dataPoints);
    var c = document.getElementById(id);
    var parent = c.parentElement;
    //console.debug("c:n id = " + c.id);

    var width = parent.offsetWidth;
    var r = width / 2 - 80;
    c.width = width;
    c.height = width;

    //Full circle
    var ctx = c.getContext("2d");
    //ctx.beginPath();
    //ctx.arc(width/2, width/2, r, 0, 2 * Math.PI);
    //ctx.stroke();

    var total = 0.0;
    for (i = 0; i < dataPoints.length; i++) {
        total += dataPoints[i].value;
    }
    console.debug("Total values: " + total);
    var progress = 0.0;
    
    for (i = 0; i < dataPoints.length; i++) {
        var angle1 = progress / total * 360.0;
        var angle2 = (progress + dataPoints[i].value) / total * 360.0;
        progress += dataPoints[i].value;
        defineArc(ctx, width / 2, width / 2, r, angle1, angle2);

        ctx.fillStyle = getRandomColor();
        ctx.fill();
    }
    progress = 0.0;
    ctx.lineWidth = 2;
    ctx.strokeStyle = "white";
    var rightLabels = [];
    var leftLabels = [];
    //white borders on the sectors, plus names
    for (i = 0; i < dataPoints.length; i++) {
        var angle1 = progress / total * 360.0;
        var angle2 = (progress + dataPoints[i].value) / total * 360.0;
        progress += dataPoints[i].value;
        defineArc(ctx, width / 2, width / 2, r, angle1, angle2);
        ctx.stroke();

        /*
        var halfAngle = (angle1 + angle2) / 2;
        var x3 = 0.9 * r * Math.sin(halfAngle / 360.0 * 2 * Math.PI);
        var y3 = 0.9 * r * Math.cos(halfAngle / 360.0 * 2 * Math.PI);
        ctx.font = "14px Courier";
        ctx.fillStyle = "black";
        ctx.textBaseline = "middle";
        ctx.textAlign = "center";
        ctx.fillText(dataPoints[i].name, width / 2 + x3, width / 2-y3);*/
        
        var halfAngle = (angle1 + angle2) / 2;
        var x3 = width/2 + 0.95 * r * Math.sin(halfAngle / 360.0 * 2 * Math.PI);
        var y3 = width/2 - 0.95 * r * Math.cos(halfAngle / 360.0 * 2 * Math.PI);
        if (halfAngle < 180) {
            //rightLabels.push(dataPoints[i].name);
            rightLabels.push(new LabelWithDataPosition(dataPoints[i].name, x3, y3));
        }
        else {
            //leftLabels.push(dataPoints[i].name);
            leftLabels.push(new LabelWithDataPosition(dataPoints[i].name, x3, y3));
        }
    }

    var spacePerLabel = c.height / leftLabels.length;
    var textY = spacePerLabel / 2;
    ctx.strokeStyle = "black";
    ctx.lineWidth = 1;
    ctx.font = "14px Courier";
    ctx.fillStyle = "black";
    ctx.textBaseline = "middle";
    for (j = leftLabels.length - 1; j >= 0; j--) {       
        ctx.textAlign = "left";
        ctx.fillText(leftLabels[j].label, 5, textY);
        ctx.beginPath();    
        ctx.moveTo(50, textY);
        ctx.lineTo(70, textY);
        ctx.lineTo(leftLabels[j].datax, leftLabels[j].datay);
        ctx.stroke();
        textY += spacePerLabel;
    }
    spacePerLabel = c.height / rightLabels.length;
    textY = spacePerLabel / 2;
    for (j = 0; j < rightLabels.length; j++) {
        ctx.textAlign = "right";
        ctx.fillText(rightLabels[j].label, width - 5, textY);
        ctx.beginPath();
        ctx.moveTo(width - 50, textY);
        ctx.lineTo(width - 70, textY);
        ctx.lineTo(rightLabels[j].datax, rightLabels[j].datay);
        ctx.stroke();
        textY += spacePerLabel;
    }

    c.onmousemove = function (e) {
        e.preventDefault();
        e.stopPropagation();

        var BB = c.getBoundingClientRect();
        offsetX = BB.left;
        offsetY = BB.top;
        console.log("OffsetX: " + offsetX + ", offsetY: " + offsetY);

        var mouseX = e.clientX - offsetX;
        var mouseY = e.clientY - offsetY;
        console.log("mousex: " + mouseX + ", mousey: " + mouseY);

        c.title = "";
        var progress = 0.0;
        for (i = 0; i < dataPoints.length; i++) {
            var angle1 = progress / total * 360.0;
            var angle2 = (progress + dataPoints[i].value) / total * 360.0;

            defineArc(ctx, width / 2, width / 2, r, angle1, angle2);
            
            if (ctx.isPointInPath(mouseX, mouseY)) {
                c.title = dataPoints[i].name + ": "+dataPoints[i].value.toFixed(0) + "(" + (dataPoints[i].value/total*100).toFixed(1) + "%)";
                /*ctx.lineWidth = 4;
                ctx.strokeStyle = "white";
                ctx.stroke();*/
                break;
            }             
            progress += dataPoints[i].value;
        }
    };
}

function defineArc(ctx, centerX, centerY, r, angle1, angle2) {
    ctx.beginPath();
    ctx.moveTo(centerX, centerY); //centering
    ctx.arc(centerX, centerY, r, degToRadians(angle1 - 90), degToRadians(angle2 - 90))
    ctx.lineTo(centerX, centerY); //back to center
    ctx.closePath();
}

function degToRadians(degrees) {
    var ret = degrees / 180.0 * Math.PI;
    if (ret < 0)
        return ret + 2 * Math.PI;
    if (ret <= 2 * Math.PI)
        return ret;
    return ret - 2 * Math.PI;
}

function rand(min, max) {
    return parseInt(Math.random() * (max - min + 1), 10) + min;
}

function getRandomColor() {
    var h = rand(0, 360);
    var s = rand(20, 80);
    var l = rand(30, 80);
    return 'hsl(' + h + ',' + s + '%,' + l + '%)';
}

class LabelWithDataPosition {
    constructor(name, x, y) {
        this.label = name;
        this.datax = x;
        this.datay = y;
    }
}
