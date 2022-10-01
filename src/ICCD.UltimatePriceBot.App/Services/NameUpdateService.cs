// <copyright file="NameUpdateService.cs" company="IOTA Content Creator DAO LLC">
// Copyright (c) IOTA Content Creator DAO LLC 2022. All rights reserved.
// Thanks to:
// Patrick -Pathin- Fischer (pfischer@daobee.org)
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using System.Runtime.InteropServices;
using System.Text;
using Discord;
using Discord.WebSocket;
using ICCD.UltimatePriceBot.App.Extensions;
using ICCD.UltimatePriceBot.App.Models;
using ReentrantAsyncLock;

namespace ICCD.UltimatePriceBot.App.Services;

/// <summary>
/// A service that updates the bot's name and Status.
/// </summary>
public class NameUpdateService
{
    private readonly Dictionary<string, uint> _updateIntervals = new Dictionary<string, uint>(StringComparer.InvariantCultureIgnoreCase);
    private readonly DiscordSocketClient _client;
    private readonly PriceDataService _priceDataService;
    private readonly string _statusLineInfo = "Powered by the ICCD";
    private Task _workerTask;
    private readonly ReentrantAsyncLock.ReentrantAsyncLock _lock = new();
    private CancellationTokenSource _cancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="NameUpdateService"/> class.
    /// </summary>
    /// <param name="client">The discord client.</param>
    /// <param name="priceDataService">The price data service.</param>
    public NameUpdateService(DiscordSocketClient client, PriceDataService priceDataService)
    {
        _updateIntervals = new Dictionary<string, uint>
            {
                { "SMR", 15000 },
                { "IOTA", 15000 },
            };

        _client = client;
        _priceDataService = priceDataService;
    }

    public async Task StartAsync()
    {
        await using (await _lock.LockAsync(CancellationToken.None))
        {
            if (_workerTask != null && !_workerTask.IsFaulted && !_workerTask.IsCanceled && !_workerTask.IsCompleted)
            {
                throw new ApplicationException("Task already started.");
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _workerTask = Work(_cancellationTokenSource.Token);
        }
    }

    public async Task StopAsync()
    {
        await using (await _lock.LockAsync(CancellationToken.None))
        {
            if (_workerTask == null || _workerTask.IsCanceled || _workerTask.IsFaulted)
            {
                throw new ApplicationException("Task not started.");
                return;
            }

            _cancellationTokenSource.Cancel();
        }
    }

    private int? _currentTokenIx = null;
    private DateTime? _lastChange = null;
    private PriceDataViewModel? _lastPriceEntry = null;

    private async Task ResetPriceInfo()
    {
        foreach (var guild in _client.Guilds)
        {
            await guild.CurrentUser.ModifyAsync(x => x.Nickname = string.Empty);
        }

        await _client.SetStatusAsync(Discord.UserStatus.Online);
        await _client.SetGameAsync(string.Empty, null, Discord.ActivityType.CustomStatus);
    }

    private Task Work(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(
            async () =>
            {
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            await ResetPriceInfo();
                        }
                        finally
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                    }

                    var tokenKeys = _updateIntervals.Keys.ToList();
                    var currentToken = _currentTokenIx != null ? tokenKeys[_currentTokenIx.Value] : tokenKeys[0];
                    var priceEntry = await _priceDataService.GetPriceDataAsync(currentToken);
                    var priceUpdated = _lastPriceEntry?.CreatedDate != priceEntry.CreatedDate;

                    if (priceUpdated)
                    {
                        var trend1HDown = priceEntry.PriceChangePercentage1Hour < 0;
                        var trend24HDown = priceEntry.PriceChangePercentage24Hours < 0;
                        // var newNickname = $"{priceEntry.Symbol.ToUpper()}: ${priceEntry.CurrentPriceUsd.GetDiscordString("N4")}";
                        // newNickname += trend1HDown ? " 📉" : " 📈";

                        var newNickname = $"{priceEntry.Name} #{priceEntry.Rank.GetDisplayString()}";

                        var sb = new StringBuilder();
                        sb = sb.Append($"${priceEntry.CurrentPriceUsd.GetDisplayString("N4")} ");

                        if (trend24HDown)
                        {
                            sb = sb.Append('⇘');
                        }
                        else
                        {
                            sb = sb.Append('⇗');
                        }
                        sb.Append($"24H: {priceEntry.PriceChangePercentage24Hours.GetDisplayString("N2")}%");
                        // sb = sb.Append($" 24H: {priceEntry.PriceChangePercentage24Hours.GetDiscordString("N2")}%");
                        // if (trend24HDown)
                        // {
                        //     sb = sb.Append("↘");
                        // }
                        // else
                        // {
                        //     sb = sb.Append("↗");
                        // }
                        // sb = sb.Append($"1H: {priceEntry.PriceChangePercentage1Hour.GetDiscordString("N2")}%");
                        // if (trend1HDown)
                        // {
                        //     sb = sb.Append("↘");
                        // }
                        // else
                        // {
                        //     sb = sb.Append("↗");
                        // }
                        var newStatus = sb.ToString();
                        foreach (var guild in _client.Guilds)
                        {
                            try
                            {
                                if (guild.CurrentUser.Nickname == null || !guild.CurrentUser.Nickname.Equals(newNickname))
                                {
                                    await guild.CurrentUser.ModifyAsync(x => x.Nickname = newNickname);
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }
                        await _client.SetGameAsync(newStatus, null, Discord.ActivityType.Playing);

                        if (trend24HDown)
                        {
                            await _client.SetStatusAsync(UserStatus.DoNotDisturb);
                        }
                        else
                        {
                            await _client.SetStatusAsync(UserStatus.Online);
                        }

                        _lastPriceEntry = priceEntry;
                    }

                    if (_lastChange == null || _currentTokenIx == null || DateTime.Now - _lastChange > TimeSpan.FromMilliseconds(_updateIntervals[_updateIntervals.Keys.ToList()[_currentTokenIx.Value]]))
                    {
                        _lastChange = DateTime.Now;
                        if (_currentTokenIx == null)
                        {
                            _currentTokenIx = 0;
                        }
                        else
                        {
                            _currentTokenIx = _currentTokenIx + 1 < _updateIntervals.Keys.Count ? _currentTokenIx + 1 : 0;
                        }
                    }

                    System.Threading.Thread.Sleep(100);
                }
            },
            cancellationToken);
    }
}