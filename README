# SignalR-Playground

This started as a new project in Visual Studio and nuget'd the StockTicker 
package into it. I forked of the SignalR/SignalR-StockTicker project and 
blew it away just for reference purposes.

I added the following things in the following order as I went about learning 
SignalR:

## Market State Messages 
Towards the bottom of the page under the section titled *Messages*
When ever the market state is changed it is broadcasts who changed it 
and the state it changed to all connected clients.  Additionally I 
overrode the OnConnected, OnDisconnected and OnReconnected hub methods 
to broadcast when a client connects or disconnects in the *Messages* list 

## Percent Changed
I updated the stock table to show percent changed "correctly" in the 
far right column.  This just updated the PercentChange getter in Stock.cs 
to divide by DayOpen rather than current Price.  It now reflects the 
percent the stock has changed since the market first opened.  It just 
felt right..

## Easy Background Task
I moved the timer contents of the StockTicker to a goofy background task based on cache 
expirations as described in this coding horror blog post 
http://blog.stackoverflow.com/2008/07/easy-background-tasks-in-aspnet/

## Send Message
Added on to the *Messages* section adding a field to broadcast messages to 
all connected clients

## Shared Cursors on Market Closed
When the market is closed it broadcasts the cursor position to all connected 
clients.  jQuery mousemove sends the cursor position to the server.  The client 
side hub then adds a div with unique id to the page that has a cursor for 
a background image.  When new positions are received the unique cursor position 
is updated.  When a client disconnects it is removed.

## Click Ghosting Broadcast
Left clicking the page sends the click position to the server for broadcast. 
Connected clients add a 'Shared Cursor' that slowly fades away and is removed. 

## Google Wave-ish Messages
Added section at the top of the page that broadcasts messages on key up as they 
are typed. Pressing Enter or clicking the 'Wave' button ends the current message.  
All connected clients see the wave messages.