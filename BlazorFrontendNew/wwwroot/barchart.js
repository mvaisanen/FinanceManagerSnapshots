function barchart(id, dataPoints) {
    //console.log("barchart() jonka id=" + id + " kutsuttu datalla:");
    //console.log(dataPoints);
    var c = document.getElementById(id);
    var parent = c.parentElement;

    var width = parent.clientWidth;
    var height = parent.clientHeight;
    //console.log("Barchart parent dims: " + width + " x " + height);
    c.width = width;
    c.height = height;

    var ctx = c.getContext("2d");

    // Set line width
    //ctx.lineWidth = 2;

    //Alalaita
    ctx.fillRect(1, height - 15, width, 2);

    var spacing = width / (dataPoints.length + 1);
    var x = spacing;

    var maxVal = 0;
    for (i = 0; i < dataPoints.length; i++) {
        if (dataPoints[i].value1 > maxVal)
            maxVal = dataPoints[i].value1;
        if (dataPoints[i].value2 > maxVal)
            maxVal = dataPoints[i].value2;
    }

    console.log("max datapoint value: " + maxVal);
    var dataP = 0;
    var baselineY = height - 15;
    
    while (x < width) {       
        ctx.fillStyle = "blue";
        ctx.strokeStyle = "blue";
        var scaledVal2 = dataPoints[dataP].value2 / maxVal * (baselineY - 30);
        ctx.fillRect(x - 12, baselineY - scaledVal2, 10, scaledVal2);

        ctx.fillStyle = "green";
        ctx.strokeStyle = "green";
        var scaledVal1 = dataPoints[dataP].value1 / maxVal * (baselineY - 30);
        ctx.fillRect(x + 2, baselineY - scaledVal1, 10, scaledVal1);

        ctx.strokeStyle = "black";
        ctx.lineWidth = 1;
        ctx.font = "12px sans-serif";
        ctx.fillStyle = "black";
        ctx.textBaseline = "middle";
        ctx.textAlign = "center"; //middle?
        ctx.fillText(dataPoints[dataP].name, x - 5, baselineY + 10, 25);
        //console.log('Piirretaan datapointtia ' + dataPoints[dataP].name + ', value1=' + dataPoints[dataP].value1 + ', value2=' + dataPoints[dataP].value2 )

        //ctx.textAlign = "right"; 
        //ctx.fillText(dataPoints[dataP].value2.toFixed(0), x - 2, baselineY - scaledVal2 - 8);
        //ctx.textAlign = "left"; 
        //ctx.fillText(dataPoints[dataP].value1.toFixed(0), x + 2, baselineY - scaledVal1 - 8);
        //Goal
        ctx.save();
        ctx.translate(x - 7, baselineY - scaledVal2 - 5); //mikä tämä on?
        ctx.rotate(0.6*Math.PI / 2);
        ctx.textAlign = "right";
        ctx.fillText(dataPoints[dataP].value2.toFixed(0), 0, 0);
        ctx.restore();
        ctx.save();

        //Actual
        ctx.translate(x + 11, baselineY - scaledVal1 - 5); //mikä tämä on?
        ctx.rotate(0.6 * Math.PI / 2);
        ctx.textAlign = "right";
        ctx.fillText(dataPoints[dataP].value1.toFixed(0), 0, 0);
        ctx.restore();
        ctx.save();

        x += spacing;
        dataP++;
    }

    


    /*
    ctx.lineWidth = 3;
    var spacing = width / (dataPoints.length + 1);
    ctx.beginPath();
    ctx.moveTo(1, height - 1);
    ctx.lineTo(width - 1, height - 1);
    ctx.closePath();
    ctx.stroke();

    var x = spacing;
    var scaledVal = 50;
    while (x < width) {
        ctx.fillRect(x - 5, scaledVal, 10, scaledVal);
        x += spacing;
    }*/


}
/*
function getRandomColor() {
    var h = rand(0, 360);
    var s = rand(20, 80);
    var l = rand(30, 80);
    return 'hsl(' + h + ',' + s + '%,' + l + '%)';
}

function adjustSize(id) {
    console.log("Adjusting barchart size...");
    var c = document.getElementById(id);
    var parent = c.parentElement;

    var width = parent.clientWidth;
    var height = parent.clientHeight; 
    c.width = width;
    c.height = height;
}

function addResizer(id) {
    console.log("addResizer() kutsuttu");
    //window.addEventListener("resize", adjustSize(id));
    document.getElementsByTagName("BODY")[0].onresize = function () { adjustSize(id) };
}*/

