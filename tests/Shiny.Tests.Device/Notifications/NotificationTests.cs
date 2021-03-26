﻿using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Shiny.Notifications;
using Xunit;


namespace Shiny.Tests.Push
{
    public class NotificationTests : IDisposable
    {
        readonly INotificationManager notificationManager;


        public NotificationTests()
        {
            ShinyHost.Init(TestStartup.CurrentPlatform, new ActionStartup
            {
                BuildServices = x => x.UseNotifications()
            });
        }


        public void Dispose()
        {
            this.notificationManager.Clear().GetAwaiter().GetResult();
            this.notificationManager.ClearChannels().GetAwaiter().GetResult();
        }


        [Fact]
        public async Task StandardTest()
        {
            await this.CreateFullChannel(nameof(StandardTest));
            await this.notificationManager.Send("Test", "Test 1", nameof(StandardTest));
        }


        [Fact]
        public async Task ChannelStoreProperTest()
        {
            var created = await this.CreateFullChannel(nameof(ChannelStoreProperTest));

            var channel = (await this.notificationManager.GetChannels()).Single();

            // TODO: compare
        }


        [Fact]
        public async Task SetChannels()
        {
            await this.CreateChannel("1");
            await this.CreateChannel("2");
            await this.CreateNotification(1, "1");
            await this.CreateNotification(2, "1");
            await this.CreateNotification(3, "2");

            await this.notificationManager.SetChannels(
                new Channel { Identifier = "1", Description = "1" },
                new Channel { Identifier = "1", Description = "3" }
            );
            var notifications = await notificationManager.GetPending();
            notifications.First(x => x.Id == 1).Channel.Should().Be("1");
            notifications.First(x => x.Id == 2).Channel.Should().Be("1");
            notifications.First(x => x.Id == 3).Channel.Should().BeNull();

            var channels = await this.notificationManager.GetChannels();
            channels.Count.Should().Be(2);
            channels.FirstOrDefault(x => x.Identifier == "1").Should().NotBeNull("1 should be found");
            channels.FirstOrDefault(x => x.Identifier == "2").Should().BeNull("2 should NOT be found");
            channels.FirstOrDefault(x => x.Identifier == "3").Should().NotBeNull("3 should be found");
        }


        [Fact]
        public async Task ClearChannelsTest()
        {
            await this.CreateChannel("1");
            await this.CreateChannel("2");
            await this.CreateChannel("3");

            await this.CreateNotification(1, "1");
            await this.CreateNotification(2, "2");
            await this.CreateNotification(3, "3");

            await this.notificationManager.ClearChannels();

            var notifications = await notificationManager.GetPending();
            notifications.Count().Should().Be(3);
            foreach (var notification in notifications)
                notification.Channel.Should().BeNull();
        }


        async Task<Channel> CreateFullChannel(string identifier)
        {
            var channel = new Channel
            {
                Identifier = identifier
            };
            await this.notificationManager.CreateChannel(channel);
            return channel;
        }


        Task CreateChannel(string name) => this.notificationManager.CreateChannel(new Channel
        {
            Identifier = name,
            Description = name
        });


        Task CreateNotification(int id, string channel)
            => this.notificationManager.Send(new Notification
            {
                Id = id,
                Title = id.ToString(),
                Message = id.ToString(),
                ScheduleDate = DateTime.Now.AddDays(30),
                Channel = channel
            });
    }
}
