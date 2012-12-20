/// <reference path="../scripts/jquery-1.8.3.js" />
/// <reference path="../scripts/jquery.signalR-1.0.0-rc1.js" />

/*!
    ASP.NET SignalR Stock Ticker Sample
*/

// Crockford's supplant method (poor man's templating)
if (!String.prototype.supplant) {
    String.prototype.supplant = function (o) {
        return this.replace(/{([^{}]*)}/g,
            function (a, b) {
                var r = o[b];
                return typeof r === 'string' || typeof r === 'number' ? r : a;
            }
        );
    };
}

// A simple background color flash effect that uses jQuery Color plugin
jQuery.fn.flash = function (color, duration) {
    var current = this.css('backgroundColor');
    this.animate({ backgroundColor: 'rgb(' + color + ')' }, duration / 2)
        .animate({ backgroundColor: current }, duration / 2);
}



$(function () {

    var ticker = $.connection.stockTicker, // the generated client-side hub proxy
        up = '▲',
        down = '▼',
        $stockTable = $('#stockTable'),
        $stockTableBody = $stockTable.find('tbody'),
        rowTemplate = '<tr data-symbol="{Symbol}"><td>{Symbol}</td><td>{Price}</td><td>{DayOpen}</td><td>{DayHigh}</td><td>{DayLow}</td><td><span class="dir {DirectionClass}">{Direction}</span> {Change}</td><td>{PercentChange}</td></tr>',
        $stockTicker = $('#stockTicker'),
        $stockTickerUl = $stockTicker.find('ul'),
        liTemplate = '<li data-symbol="{Symbol}"><span class="symbol">{Symbol}</span> <span class="price">{Price}</span> <span class="change"><span class="dir {DirectionClass}">{Direction}</span> {Change} ({PercentChange})</span></li>',
        cursorTemplate = '<div id="tmpId" class="sharedCursor">&nbsp;</div>',
        waveTemplate = '<li id="tmpWaveId"></li>',
        shareCursor = false;


    function formatStock(stock) {
        return $.extend(stock, {
            Price: stock.Price.toFixed(2),
            PercentChange: (stock.PercentChange * 100).toFixed(2) + '%',
            Direction: stock.Change === 0 ? '' : stock.Change >= 0 ? up : down,
            DirectionClass: stock.Change === 0 ? 'even' : stock.Change >= 0 ? 'up' : 'down'
        });
    }

    function scrollTicker() {
        var w = $stockTickerUl.width();
        $stockTickerUl.css({ marginLeft: w });
        $stockTickerUl.animate({ marginLeft: -w }, 15000, 'linear', scrollTicker);
    }

    function stopTicker() {
        $stockTickerUl.stop();
    }

    function init() {
        return ticker.server.getAllStocks().done(function (stocks) {
            $stockTableBody.empty();
            $stockTickerUl.empty();
            $.each(stocks, function () {
                var stock = formatStock(this);
                $stockTableBody.append(rowTemplate.supplant(stock));
                $stockTickerUl.append(liTemplate.supplant(stock));
            });
        });
    }

    // Add client-side hub methods that the server will call
    $.extend(ticker.client, {
        updateStockPrice: function (stock) {
            var displayStock = formatStock(stock),
                $row = $(rowTemplate.supplant(displayStock)),
                $li = $(liTemplate.supplant(displayStock)),
                bg = stock.LastChange === 0
                    ? '255,216,0' // yellow
                    : stock.LastChange > 0
                        ? '154,240,117' // green
                        : '255,148,148'; // red

            $stockTableBody.find('tr[data-symbol=' + stock.Symbol + ']')
                .replaceWith($row);
            $stockTickerUl.find('li[data-symbol=' + stock.Symbol + ']')
                .replaceWith($li);

            $row.flash(bg, 1000);
            $li.flash(bg, 1000);
        },

        marketOpened: function () {
            $("#open").prop("disabled", true);
            $("#close").prop("disabled", false);
            $("#reset").prop("disabled", true);
            scrollTicker();
            shareCursor = false;
        },

        marketClosed: function () {
            $("#open").prop("disabled", false);
            $("#close").prop("disabled", true);
            $("#reset").prop("disabled", false);
            stopTicker();
            shareCursor = true;
        },

        marketReset: function () {
            return init();
        },

        postMessage: function (msg) {
            $("#msg").prepend("<li>" + msg + "</li>");
        },

        catchWave: function (id, wave, complete) {
            if (!$("#" + id + "wave").length) {
                $("#wavePool").prepend(waveTemplate);
                $("#tmpWaveId").attr("id", id + "wave");
            }
            $("#" + id + "wave").text(wave);
            if (complete) {
                $("#" + id + "wave").removeAttr("id");
            }

        },

        updateSharedCursor: function (id, x, y) {
            //debugger;
            if (!$("#" + id + "cursor").length) {
                $("#sharedCursors").append(cursorTemplate);
                $("#tmpId").attr("id", id + "cursor");
            }

            $("#" + id + "cursor").css("top", y + "px");
            $("#" + id + "cursor").css("left", x + "px");
        },

        deleteSharedCursor: function (id) {
            $("#" + id + "cursor").remove();
        },

        receiveClick: function (x, y) {
            if (!$("#" + x + "" + y + "cursor").length) {
                $("#sharedCursors").append(cursorTemplate);
                $("#tmpId").attr("id", x + "" + y + "cursor");
            }
            $("#" + x +""+ y + "cursor").css("top", y + "px");
            $("#" + x + "" + y + "cursor").css("left", x + "px");
            $("#" + x + "" + y + "cursor").fadeOut(1300, function () { $(this).remove(); });
        }

    });

    // Start the connection
    $.connection.hub.start()
        .pipe(init)
        .pipe(function () {
            return ticker.server.getMarketState();
        })
        .done(function (state) {
            if (state === 'Open') {
                ticker.client.marketOpened();
            } else {
                ticker.client.marketClosed();
            }

            // Wire up the buttons
            $("#open").click(function () {
                ticker.server.openMarket();
            });

            $("#close").click(function () {
                ticker.server.closeMarket();
            });

            $("#reset").click(function () {
                ticker.server.reset();
            });

            $("#sendButton").click(function () {
                var msg = $("#sendMessage").val();
                ticker.server.sendMessage(msg);
                $("#sendMessage").val("");
            });

            $("#waveMessage").keyup(function (e) {
                var complete = e.which == 13;
                var msg = $("#waveMessage").val();
                //debugger;                
                ticker.client.catchWave(ticker.connection.id, msg, complete);
                ticker.server.sendWave(msg, complete);
                if (complete)
                    $("#waveMessage").val("");
            });

            $(document).click(function (e) {
                ticker.server.sendClick(e.pageX, e.pageY);
                //ticker.client.catchWave(ticker.connection.id, e.pageX+","+e.pageY, true);
            });

            $("#waveButton").click(function () {
                var msg = $("#waveMessage").val();
                ticker.server.sendWave(msg, true);
                $("#waveMessage").val("");
            });

            var mousesharecount = 0;
            $(document).mousemove(function (event) {
                if (shareCursor) {
                    mousesharecount++;
                    if (mousesharecount % 2) {
                        ticker.server.uploadCursor(event.pageX, event.pageY);
                    }
                }
            });

        });
});