// If you're here, I want you to know any way to break
// the trading system would be within the backend and
// not freakin Javascript!

// Default trade state
var tradeState = "BUY";
var lastChart = "none";
var price = 0;
var marketRate = false;
var api_key = "none";
var account_id = "none";
var ticker = "whoops";
var owned = 0;
var shiftTime = 60;
var refresh;
var chart = null;
var volumechart = null;
var low = 0;
var high = 0;
var chartColors = [];
var threeAvg = [];
var fiveLag = [];
var gotMessages = false;

function SetUser(account, key, curowned) {
    api_key = key;
    account_id = account;
    owned = curowned;
}

function BuildThreeAvg(history) {

    threeAvg = [];

    // Moving average (3-point)
    for (var i = 6; i < 65; i++) {
        var avg = BuildAvgPoint(history, i, 3);
        threeAvg.push(avg);
    }
}

function BuildAvgPoint(history, index, depth) {
    var avg = 0;

    var amount = (depth - 1) / 2;

    avg += history[index];

    for (var i = 0; i < amount; i++) {
        avg += history[index - (i + 1)];
        avg += history[index + (i + 1)];
    }

    return avg / depth;
}

function BuildLagPoint(history, index, depth) {
    var avg = 0;

    for (var i = 0; i < depth; i++) {
        avg += history[index - i];
    }

    return avg / depth;
}

function BuildFiveLag(history) {

    fiveLag = [];

    // Moving average (5-point)
    for (var i = 6; i < 65; i++) {
        var avg = BuildLagPoint(history, i, 5);
        fiveLag.push(avg);
    }
}

function BuildChart(tick, time) {

    var chartTime = time;
    ticker = tick;

    console.log("Building " + chartTime + " chart.");

    lastChart = chartTime;

    var timesplit = chartTime.split('x');

    var multiplier = 1;
    if (timesplit[1] == "HOUR") {
        multiplier = 60;
    }
    else if (timesplit[1] == "DAY") {
        multiplier = 60 * 24;
    }

    if (refresh != null) {
        clearInterval(refresh);
    }

    refresh = setInterval(GraphRefresh, multiplier * 60000);

    shiftTime = multiplier * timesplit[0];

    $.get('/Api/Eco/GetStockHistory?ticker=' + ticker + '&type=' + timesplit[1] + '&count=65&interval=' + timesplit[0], function (out) {
        var history = out;

        console.log(out);
        console.log(history.slice(Math.max(history.length - 60, 1)));

        console.log(out.length);

        history.push(price);

        BuildThreeAvg(history);
        BuildFiveLag(history);

        chartColors = [];

        // Colors
        chartColors.push('rgba(0, 255, 0, 255)');
        for (var i = 5; i < 60; i++) {
            if (history[i] > history[i + 1]) {
                chartColors.push('rgba(255, 0, 0, 255)');
            }
            else if (history[i] < history[i + 1]) {
                chartColors.push('rgba(0, 255, 0, 255)');
            }
            else {
                chartColors.push('rgba(138, 138, 138, 1.0)');
            }
        }

        if (chart != null) {
            chart.data.datasets[2].data = history.slice(Math.max(history.length - 60, 1));
            chart.data.datasets[0].data = threeAvg;
            chart.data.datasets[1].data = fiveLag;
            chart.update();
        }
        else {

            var data = {
                datasets: [
                    {
                        data: threeAvg,
                        borderColor: 'rgba(252, 186, 3, 1.0)',
                        backgroundColor: 'rgba(0, 0, 0, 0.0)',
                        pointBackgroundColor: 'rgba(0, 0, 0, 0.0)',
                        pointBorderColor: 'rgba(0, 0, 0, 0.0)',
                        borderWidth: 0.7,
                        lineTension: 0.5
                    },
                    {
                        data: fiveLag,
                        borderColor: 'rgba(252, 3, 215, 1.0)',
                        backgroundColor: 'rgba(0, 0, 0, 0.0)',
                        pointBackgroundColor: 'rgba(0, 0, 0, 0.0)',
                        pointBorderColor: 'rgba(0, 0, 0, 0.0)',
                        borderWidth: 0.7,
                        lineTension: 0.5
                    },
                    {
                        data: history.slice(Math.max(history.length - 60, 1)),
                        borderColor: 'rgba(26, 217, 77, 1.0)',
                        backgroundColor: 'rgba(0, 0, 0, 0.0)',
                        pointBackgroundColor: 'rgba(0, 0, 0, 0.0)',
                        pointBorderColor: 'rgba(0, 0, 0, 0.0)',
                        lineTension: 0
                    },
                ],

                //These labels appear in the legend and in the tooltips when hovering different arcs
                labels: Array.from({ length: 60 }, (x, i) => -59 + i),
            };

            // console.log(data);

            options = {
                scales: {
                    yAxes: [{
                        position: 'right',
                        ticks: {
                            //beginAtZero: true
                        },
                    }]
                },
                legend: {
                    display: false
                },
            }

            var ctx = $('#priceChart');
            chart = new Chart(ctx, {
                type: 'line',
                data: data,
                responsive: true,
                options: options
            });
        }



        $.get('/Api/Eco/GetStockVolumeHistory?ticker=' + ticker + '&type=' + timesplit[1] + '&count=60&interval=' + timesplit[0], function (out) {

            var volumehistory = out;

            if (volumechart != null) {
                volumechart.data.datasets[0].data = volumehistory;
                volumechart.update();
            }
            else {
                var volumehistory = out;

                var volumedata = {
                    datasets: [{
                        data: volumehistory,
                        borderColor: chartColors,
                        backgroundColor: chartColors,
                        lineTension: 0
                    }],

                    labels: Array.from({ length: 60 }, (x, i) => -59 + i),
                };

                // console.log(data);

                volumeoptions = {
                    scales: {
                        yAxes: [{
                            position: 'right',
                            ticks: {
                                beginAtZero: true,
                                stepSize: 1
                            },
                        }]
                    },
                    legend: {
                        display: false
                    },
                }

                var ctx = $('#volumeChart');
                volumechart = new Chart(ctx, {
                    type: 'bar',
                    data: volumedata,
                    responsive: true,
                    options: volumeoptions
                });
            }
        });

    });
};

function GraphRefresh() {
    chart.data.datasets[2].data.shift();
    chart.data.datasets[2].data.push(price);
    chart.data.datasets[0].data.shift();
    chart.data.datasets[0].data.push(BuildAvgPoint(chart.data.datasets[2].data, chart.data.datasets[2].data.length - 2, 3));
    chart.data.datasets[1].data.shift();
    chart.data.datasets[1].data.push(BuildLagPoint(chart.data.datasets[2].data, chart.data.datasets[2].data.length - 1, 5));


    volumechart.data.datasets[0].data.shift();
    volumechart.data.datasets[0].data.push(0);

    chartColors.shift();
    chartColors.push('rgba(138, 138, 138, 1.0)');

    chart.update();
    volumechart.update();
}

var buyButton = $('#buy_button');
var sellButton = $('#sell_button');

SetTradeState(tradeState);

// Button behavior
function SetTradeState(state) {

    tradeState = state;

    if (tradeState == "BUY") {
        buyButton.css("background-color", "#058224");
        sellButton.css("background-color", "#222222");
    }

    if (tradeState == "SELL") {
        buyButton.css("background-color", "#222222");
        sellButton.css("background-color", "#820404");
    }
}

var priceInput = $('#price-input');
var amountInput = $('#amount-input');
var priceTop = $('#price-top');
var priceQueue = $('#price-queue');
var priceLeft = $('#price-left');
var marketCheckbox = $('#market-check');
var sellBox = $('#sellBox');
var buyBox = $('#buyBox');

function UpdatePrice(newPrice, tradePrice) {
    price = newPrice;

    var formatted = FormatPrice(newPrice);
    var formattrade = FormatPrice(tradePrice);

    // priceInput.val(formatted);
    priceTop.text('¢' + formatted);
    priceQueue.text('¢' + formattrade);


    if (chart != null) {
        chart.data.datasets[2].data[59] = newPrice;
        chart.update();
    }
}

function UpdateSpread() {
    var avg = (low + high) / 2;

    var percent = Math.round((low / high) * 100) - 100;

    priceLeft.html('¢' + FormatPrice(avg) + ' (' + percent + '%)');
}

function FormatPrice(input) {
    return (Math.round(input * 100) / 100).toFixed(2);
}

function ToggleMarketRate() {

    marketRate = marketCheckbox.is(":checked");

    priceInput.prop("disabled", marketRate);

    if (marketRate) {
        priceInput.val('');
    }
}

function SubmitTrade() {
    var chosenPrice = 0;
    var amount = 0;

    if (!marketRate) {
        chosenPrice = priceInput.val();
    }

    if (amountInput.val()) {
        amount = amountInput.val();
    }

    if (tradeState == "BUY") {
        $.get('/Api/Eco/SubmitStockBuy?ticker=' + ticker + '&count=' + amount + '&price=' + chosenPrice + '&accountid=' + account_id + '&auth=' + api_key, function (out) {
            console.log(out);
        });
    }

    if (tradeState == "SELL") {
        $.get('/Api/Eco/SubmitStockSell?ticker=' + ticker + '&count=' + amount + '&price=' + chosenPrice + '&accountid=' + account_id + '&auth=' + api_key, function (out) {
            console.log(out);
        });
    }
}

var connection;

function PrepSignalR() {
    // SIGNALR HOOKS //
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/ExchangeHub")
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Retry connection if failed
    connection.onclose(async () => {
        await ConnectSignalR();
    });

    connection.on("StockOffer", (offer) => {

        offer = JSON.parse(offer);

        console.log(offer);

        if (offer.Ticker != ticker) {
            return;
        }

        if (offer.Order_Type == "BUY") {
            BuildQueueBuy();
        }
        else {
            BuildQueueSell();

            if (offer.Owner_Id == account_id) {
                owned -= offer.Amount;
                UpdateOwned(owned);
            }
        }

        console.log(offer.Owner_Id);

        if (offer.Owner_Id == account_id) {

            UpdateBalance();

            if (offer.Order_Type == "BUY") {
                AddOpenBuyOrder(offer);
            }
            else {
                AddOpenSellOrder(offer);
            }
        }
    });

    connection.on("StockOfferCancel", (offer) => {

        offer = JSON.parse(offer);

        if (offer.Ticker != ticker) {
            return;
        }

        console.log(offer);

        if (offer.Order_Type == "BUY") {
            BuildQueueBuy();
        }
        else {
            BuildQueueSell();

            if (offer.Owner_Id == account_id) {
                owned += offer.Amount;
                UpdateOwned(owned);
            }
        }

        if (offer.Owner_Id == account_id) {

            UpdateBalance();

            var open = $('#open-' + offer.Id);

            if (open == null) {
                open = $('#open-' + offer.Id);
            }

            if (open != null) {
                open.remove();
            }
        }
    });

    connection.on("StockTrade", (trade) => {

        var tradeObj = JSON.parse(trade);

        console.log(tradeObj)

        if (tradeObj.Ticker == ticker) {

            BuildQueue();

            UpdatePrice(tradeObj.True_Price, tradeObj.Price);

            chart.data.datasets[0].data[58] = (BuildAvgPoint(chart.data.datasets[2].data, chart.data.datasets[2].data.length - 2, 3));
            chart.data.datasets[1].data[59] = (BuildLagPoint(chart.data.datasets[2].data, chart.data.datasets[2].data.length - 1, 5));

            volumechart.data.datasets[0].data[59] += tradeObj.Amount;

            if (chart.data.datasets[2].data[59] < chart.data.datasets[2].data[58]) {
                chartColors[59] = 'rgba(255, 0, 0, 255)';
            }
            else if (chart.data.datasets[2].data[59] > chart.data.datasets[2].data[58]) {
                chartColors[59] = 'rgba(0, 255, 0, 255)';
            }
            else {
                chartColors[59] = 'rgba(138, 138, 138, 1.0)';
            }

            volumechart.update();

            if (tradeObj.To == account_id) {
                owned += tradeObj.Amount;
                UpdateOwned(owned);
            }

            if (tradeObj.From == account_id ||
                tradeObj.To == account_id) {
                UpdateBalance();

                var openbuy = $('#open-' + tradeObj.Buy_Id);
                var opensell = $('#open-' + tradeObj.Sell_Id);

                if (openbuy != null) {
                    openbuy.remove();
                }

                if (opensell != null) {
                    opensell.remove();
                }
            }
        }
    });

    connection.on("RecieveMessage", (message, mode) => {
        AddChatMessage(message, mode);

        chatView.scrollTop(chatView[0].scrollHeight);
    });

    connection.on("RecieveMessageHistory", (messages, modes) => {

        console.log(messages);

        for (var i = 0; i < messages.length; i++) {
            AddChatMessage(messages[i], modes[i]);
        }

        chatView.scrollTop(chatView[0].scrollHeight);
    });
}

var owned_text = $("#owned-text");
var balance_text = $("#balance-text");

function UpdateOwned(newOwned) {
    owned_text.html('You own ' + newOwned + ' ' + ticker);
}

function UpdateBalance() {
    $.get('/Api/Eco/GetBalance?svid=' + account_id, function (out) {
        balance_text.html('You have ¢' + FormatPrice(out));
    });
}

async function ConnectSignalR() {

    try {
        await connection.start();
        console.log("Connected to Exchange SignalR Hub");
        GetMessageHistory();
    } catch (err) {
        console.log("Failed to connect to SignalR> Retrying in 5 seconds...");
        console.log(err);
        setTimeout(() => ConnectSignalR(), 5000);
    }
}

function BuildQueue() {
    BuildQueueSell();
    BuildQueueBuy();
}

var buyTotal = $('#buy-total');
var sellTotal = $('#sell-total');

function BuildQueueSell() {
    $.get('/Api/Eco/GetQueueInfo?ticker=' + ticker + '&type=SELL', function (out) {

        var queueData = JSON.parse(out);
        var total = 0;

        if (queueData.length > 0) {
            low = queueData[queueData.length - 1].Target;
            console.log(low);
        }

        for (var i = 0; i < 10; i++) {
            var priceText = $('#queue-sell-price-' + i);
            var amountText = $('#queue-sell-amount-' + i);
            var totalText = $('#queue-sell-total-' + i);

            if (i < queueData.length) {
                priceText.html('¢' + FormatPrice(queueData[i].Target));
                priceText.click({ value: queueData[i].Target }, SetPriceInput);
                amountText.html(queueData[i].Amount);
                totalText.text('¢' + FormatPrice(queueData[i].Target * queueData[i].Amount));

                total += queueData[i].Target * queueData[i].Amount;
            }
            else {
                priceText.html('');
                amountText.html('');
                totalText.html('');
            }
        }

        total = Math.round(total / 1000);

        sellTotal.text('¢' + total + 'k');

        UpdateSpread();
    });
}

function BuildQueueBuy() {
    $.get('/Api/Eco/GetQueueInfo?ticker=' + ticker + '&type=BUY', function (out) {

        var queueData = JSON.parse(out);

        var total = 0;

        if (queueData.length > 0) {
            high = queueData[0].Target;
            console.log(high);
        }

        for (var i = 0; i < 10; i++) {
            var priceText = $('#queue-buy-price-' + i);
            var amountText = $('#queue-buy-amount-' + i);
            var totalText = $('#queue-buy-total-' + i);

            if (i < queueData.length) {
                priceText.html('¢' + FormatPrice(queueData[i].Target));
                priceText.click({ value: queueData[i].Target }, SetPriceInput);
                amountText.html(queueData[i].Amount);
                totalText.text('¢' + FormatPrice(queueData[i].Target * queueData[i].Amount));

                total += queueData[i].Target * queueData[i].Amount;
            }
            else {
                priceText.html('');
                amountText.html('');
                totalText.html('');
                priceText.click(console.log('No price linked'));
            }
        }

        total = Math.round(total / 1000);

        buyTotal.text('¢' + total + 'k');

        UpdateSpread();
    });
}

function SetPriceInput(e) {
    priceInput.val(e.data.value);
}


var order_holder = $('#order-holder');

function AddAllOrders() {
    $.getJSON('/Api/Eco/GetUserStockOffers?ticker=' + ticker + '&svid=' + account_id, function (out) {
        console.log(out);
        out.forEach(AddOrder);
    });
}

function AddOrder(order) {
    if (order.Order_Type == "BUY") {
        AddOpenBuyOrder(order);
    }
    else {
        AddOpenSellOrder(order);
    }
}

function AddOpenBuyOrder(order) {
    var domText =
        '<div id="open-' + order.Id + '">' +
        '<div style="display:inline-block">' +
        '<h5 class="offer-text money-green">Buy: </h5>' +
        '<h5>' + order.Amount + ' ' + order.Ticker + '@¢' + order.Target + '</h5>' +
        '</div>' +
        '<div style="display:inline-block">' +
        '<button onclick="CancelOrder(\'' + order.Id + '\')" class="btn btn-outline-danger ml-3 mb-4">Cancel</button>' +
        '</div>' +
        '</div>';

    order_holder.append($(domText));
}

function AddOpenSellOrder(order) {
    var domText =
        '<div id="open-' + order.Id + '">' +
        '<div style="display:inline-block">' +
        '<h5 class="offer-text money-red">Sell: </h5>' +
        '<h5>' + order.Amount + ' ' + order.Ticker + '@¢' + order.Target + '</h5>' +
        '</div>' +
        '<div style="display:inline-block">' +
        '<button onclick="CancelOrder(\'' + order.Id + '\')" class="btn btn-outline-danger ml-3 mb-4">Cancel</button>' +
        '</div>' +
        '</div>';

    order_holder.append($(domText));
}

function CancelOrder(orderid) {
    console.log("Cancelling order " + orderid);

    $.getJSON('/Api/Eco/CancelOrder?orderid=' + orderid + '&accountid=' + account_id + '&auth=' + api_key, function (out) {
        console.log(out);
    });
}

var chatInput = $('#chat-input');
var chatView = $('#chat-view');

function AddChatMessage(message, mode) {
    var dom = '';

    if (mode == "BUY") {
        dom = $('<p class="chat-message" style="color:#a5ff9e"></p>');
    }
    else {
        dom = $('<p class="chat-message" style="color:#ffa59e"></p>');
    }

    dom.text(message);
    chatView.append(dom);
}

chatInput.on('keyup', function (e) {
    if (e.keyCode === 13) {

        if (chatInput.val() == '') {
            return;
        }

        var message = chatInput.val();
        chatInput.val('');

        connection.invoke("SendMessage", account_id, api_key, message, ticker, tradeState).catch(function (err) {
            return console.error(err.toString());
        });
    }
});

function GetMessageHistory() {

    if (gotMessages) return;

    connection.invoke("RequestHistory").catch(function (err) {
        return console.error(err.toString());
    });

    gotMessages = true;
}

// Ownership chart

var r = 60;
var g = 255;
var b = 100;
var a = 1.0;

var totalStock = $('#totalStock');
var ownedPerc = $('#ownedPerc');

function BuildOwnership() {
    $.get('/Api/Eco/GetOwnerData?ticker=' + ticker, function (out) {
        var ownerData = out;

        var names = [];
        var amounts = [];
        var colors = [];
        var borders = [];

        var total = 0;

        console.log(ownerData);

        for (i = 0; i < ownerData.length; i++) {
            total += ownerData[i].amount;
            names.push(ownerData[i].ownerName);
            amounts.push(ownerData[i].amount);
        }

        totalStock.text('Total shares: ' + total);
        ownedPerc.text('You own: ' + FormatPrice((owned / total) * 100.0) + '%');

        function MakeColor() {
            colors.push('rgba(' + r + ',' + g + ',' + b + ',' + a + ')');
            borders.push('rgba(0, 0, 0, 0.3)');
            g = g - 10;
            b = b - 10;
            a = a - 0.05;
            console.log("Hello?");
        }

        // Create colors
        names.forEach(e => MakeColor());

        ownerdata = {
            datasets: [{
                data: amounts,
                borderColor: borders.reverse(),
                backgroundColor: colors.reverse()
            }],

            // These labels appear in the legend and in the tooltips when hovering different arcs
            labels: names,

        };

        owneroptions = {
            legend: {
                display: false

            },
            cutoutPercentage: 70
        }

        var own = $('#ownership');
        var ownership = new Chart(own, {
            type: 'doughnut',
            data: ownerdata,
            options: owneroptions
        });
    });
}

/*
var odata = he.decode('@data').split(',');
var ldata = he.decode('@labels').split(',');
var colors = [];
var borders = [];

var r = 60;
var g = 255;
var b = 100;
var a = 1.0;

odata.forEach(e => MakeColor());

function MakeColor() {
    colors.push('rgba(' + r + ',' + g + ',' + b + ',' + a + ')');
    borders.push('rgba(0, 0, 0, 0.3)');
    g = g - 10;
    b = b - 10;
    a = a - 0.05;
    console.log("Hello?");
}

console.log(colors);

// Ownership chart
ownerdata = {
    datasets: [{
        data: odata,
        borderColor: borders.reverse(),
        backgroundColor: colors.reverse()
    }],

    // These labels appear in the legend and in the tooltips when hovering different arcs
    labels: ldata,

};

owneroptions = {
    legend: {
        display: false

    },
    cutoutPercentage: 70
}

var own = $('#ownership');
var ownership = new Chart(own, {
    type: 'doughnut',
    data: ownerdata,
    options: owneroptions
});
*/