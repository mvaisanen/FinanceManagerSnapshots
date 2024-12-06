function stockhistorygraph(id, dividendData, priceData) {
    console.log("stockhistorygraph(), divdata:");
    console.log(dividendData);
    console.log("stockhistorygraph(), pricedata:");
    console.log(priceData);
    var c = document.getElementById(id);
    var parent = c.parentElement;

    var width = parent.clientWidth;
    var height = parent.clientHeight;
    c.width = width;
    c.height = height;
    console.log("width " + width + ", height " + height);

    var ctx = c.getContext("2d");
    ctx.fillStyle = "black";
    ctx.strokeStyle = "black";


    //Alalaita
    ctx.fillRect(1, height - 15, width, 2);

    var priceDict = {};

    console.log("calculating min/max values...");
    var maxDiv = 0;
    var minDate = Date.parse(dividendData[0].paymentDate);
    var maxDate = Date.parse(dividendData[dividendData.length-1].paymentDate);
    for (i = 0; i < dividendData.length; i++) {       
        if (dividendData[i].amountPerShare > maxDiv)
            maxDiv = dividendData[i].amountPerShare;
    }
    console.log("calculating min/maxPrice...");
    var maxPrice = 0;
    var minPrice = 10000000;
    for (i = 0; i < priceData.length; i++) {
        if (priceData[i].closePrice > maxPrice)
            maxPrice = priceData[i].closePrice;
        if (priceData[i].closePrice < minPrice)
            minPrice = priceData[i].closePrice;
        priceDict[Date.parse(priceData[i].date)] = priceData[i];
    }
    console.log("Comparing price dates to div dates...");
    var priceMinDate = Date.parse(priceData[0].date);
    if (priceMinDate < minDate)
        minDate = priceMinDate;
    var priceMaxDate = Date.parse(priceData[priceData.length-1].date)
    if (priceMaxDate > maxDate)
        maxDate = priceMaxDate;
    console.log("Laskettu max dividend: " + maxDiv + ", maxPrice " + maxPrice + ", minDate " + minDate + ", maxDate " + maxDate);
    var pixelsPerTick = width * 1.0 / (1.0*(maxDate - minDate));

    var yieldSum = 0;
    var yieldPoints = 0;
    console.log("Starting drawing. Dividends...");
    for (i = 0; i < dividendData.length; i++) {
        var rawValue = dividendData[i].amountPerShare;
        var date = Date.parse(dividendData[i].paymentDate);
        var x = (date-minDate) * pixelsPerTick;
        var scaledVal = (rawValue / maxDiv / 4) * height;
        //console.log("Piirretaan fillRect " + (x - 2) + ", " + (height-15 - scaledVal) + ", 4, " + scaledVal);
        ctx.fillRect(x - 2, height - 15 - scaledVal, 4, scaledVal);
        if (priceDict[date] != undefined) {
            yieldSum = yieldSum + rawValue * 4.0 / (priceDict[date].closePrice) * 100.0; //*4.0 is to get annual div from quarterly. Needs some logic for once-a-year divs etc
            yieldPoints++;
        }
    }

    console.log("Drawing areas...");
    var averYield = yieldSum / yieldPoints;
    console.log("Average yield: " + averYield);
    ctx.strokeStyle = "green";
    ctx.moveTo(0, height-15);
    for (i = 0; i < dividendData.length; i++) {
        var fairValue = dividendData[i].amountPerShare * 4 / (averYield/100.0);// yield = div / price  -> price = div / yield
        console.log("fairValue: " + fairValue);
        var fairValueScaled = fairValue / maxPrice * (height - 50);
        var date = Date.parse(dividendData[i].paymentDate);
        var x = (date - minDate) * pixelsPerTick;
        console.log("ctx.lineTo(" + x + "," + fairValueScaled + ");");
        ctx.lineTo(x, height - fairValueScaled);
    }
    ctx.stroke();


    ctx.strokeStyle = "black";
    console.log("Drawing prices...");  
    var first = true;
    for (i = 0; i < priceData.length; i++) {
        var price = priceData[i].closePrice;
        //var scaledPrice = price / maxPrice * (height - 50); //todo: actual scaling to match yield regions, this just for testing
        var scaledPriceVal = price / maxPrice * (height - 50);
        var date = Date.parse(priceData[i].date);
        var x = (date - minDate) * pixelsPerTick;
        if (first) {
            ctx.moveTo(x, height - scaledPriceVal);
            first = false;
        }
        else
            ctx.lineTo(x,height - scaledPriceVal)
    }
    ctx.stroke();
    console.log("Pitäisi olla piirretty ja valmista");
