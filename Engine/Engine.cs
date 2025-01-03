using System;

namespace Alga.search;

/// <summary>
/// 
/// </summary>
public class Engine
{
    public Engine() {

    }

    public async Task StartAsync() {
        while (true) {
            await Task.Delay(300000);
            Words.DeleteOutdate();
        }
    }
}
