using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DCL.Interface;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DCL.Chat.Channels
{
    public class ChatChannelsControllerMock : IChatController
    {
        private readonly ChatController controller;
        private readonly UserProfileController userProfileController;

        public event Action<ChatMessage> OnAddMessage
        {
            add => controller.OnAddMessage += value;
            remove => controller.OnAddMessage -= value;
        }
        public event Action OnInitialized
        {
            add => controller.OnInitialized += value;
            remove => controller.OnInitialized -= value;
        }
        
        public event Action<Channel> OnChannelUpdated
        {
            add => controller.OnChannelUpdated += value;
            remove => controller.OnChannelUpdated -= value;
        }
        public event Action<Channel> OnChannelJoined
        {
            add => controller.OnChannelJoined += value;
            remove => controller.OnChannelJoined -= value;
        }
        public event Action<string, string> OnJoinChannelError
        {
            add => controller.OnJoinChannelError += value;
            remove => controller.OnJoinChannelError -= value;
        }
        public event Action<string> OnChannelLeft
        {
            add => controller.OnChannelLeft += value;
            remove => controller.OnChannelLeft -= value;
        }
        public event Action<string, string> OnChannelLeaveError
        {
            add => controller.OnChannelLeaveError += value;
            remove => controller.OnChannelLeaveError -= value;
        }
        public event Action<string, string> OnMuteChannelError
        {
            add => controller.OnMuteChannelError += value;
            remove => controller.OnMuteChannelError -= value;
        }

        public int TotalJoinedChannelCount => 5;

        public ChatChannelsControllerMock(
            ChatController controller,
            UserProfileController userProfileController)
        {
            this.controller = controller;
            this.userProfileController = userProfileController;
        }

        public List<ChatMessage> GetAllocatedEntries() => controller.GetAllocatedEntries();

        public List<ChatMessage> GetPrivateAllocatedEntriesByUser(string userId) =>
            controller.GetPrivateAllocatedEntriesByUser(userId);

        public void Send(ChatMessage message)
        {
            controller.Send(message);

            SimulateDelayedResponseFor_JoinChatMessage(message.body).Forget();
        }

        private async UniTask SimulateDelayedResponseFor_JoinChatMessage(string chatMessage)
        {
            await UniTask.Delay(Random.Range(1000, 3000));

            string chatMessagerToLower = chatMessage.ToLower();

            if (chatMessagerToLower.StartsWith("/join "))
            {
                if (!chatMessagerToLower.Contains("error"))
                    controller.JoinChannelConfirmation(CreateMockedDataFor_ChannelInfoPayload(chatMessage));
                else
                    controller.JoinChannelError(CreateMockedDataFor_JoinChannelErrorPayload(chatMessage));
            }
        }

        private string CreateMockedDataFor_ChannelInfoPayload(string joinMessage)
        {
            ChannelInfoPayload mockedPayload = new ChannelInfoPayload
            {
                channelId = joinMessage.Split(' ')[1].Replace("#", ""),
                unseenMessages = 0,
                memberCount = 10,
                joined = true,
                muted = false
            };

            return JsonUtility.ToJson(mockedPayload);
        }

        private string CreateMockedDataFor_JoinChannelErrorPayload(string joinMessage)
        {
            JoinChannelErrorPayload mockedPayload = new JoinChannelErrorPayload
            {
                channelId = joinMessage.Split(' ')[1].Replace("#", ""),
                message = "There was an error creating the channel."
            };

            return JsonUtility.ToJson(mockedPayload);
        }

        public void MarkMessagesAsSeen(string userId) => controller.MarkMessagesAsSeen(userId);

        public void GetPrivateMessages(string userId, int limit, long fromTimestamp) =>
            controller.GetPrivateMessages(userId, limit, fromTimestamp);

        public void JoinOrCreateChannel(string channelId) => JoinOrCreateFakeChannel(channelId).Forget();

        private async UniTask JoinOrCreateFakeChannel(string channelId)
        {
            await UniTask.Delay(Random.Range(40, 1000));
            
            var msg = new ChannelInfoPayload
            {
                joined = true,
                channelId = channelId,
                muted = false,
                memberCount = Random.Range(0, 16),
                unseenMessages = Random.Range(0, 16)
            };
            controller.JoinChannelConfirmation(JsonUtility.ToJson(msg));
        }

        public void LeaveChannel(string channelId) => LeaveFakeChannel(channelId).Forget();

        private async UniTask LeaveFakeChannel(string channelId)
        {
            await UniTask.Delay(Random.Range(40, 1000));
            
            var msg = new ChannelInfoPayload
            {
                joined = false,
                channelId = channelId,
                muted = false,
                memberCount = Random.Range(0, 16),
                unseenMessages = Random.Range(0, 16)
            };
            controller.JoinChannelConfirmation(JsonUtility.ToJson(msg));
        }

        public void GetChannelMessages(string channelId, int limit, long fromTimestamp) =>
            GetFakeMessages(channelId, limit, fromTimestamp).Forget();

        private async UniTask GetFakeMessages(string channelId, int limit, long fromTimestamp)
        {
            await UniTask.Delay(Random.Range(40, 1000));
            
            var characters = new[]
                {'a', 'A', 'b', 'B', 'c', 'C', 'd', 'D', 'e', 'E', 'f', 'F', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};

            for (var i = 0; i < Random.Range(0, limit); i++)
            {
                var userId = "fake-user";
                for (var x = 0; x < 4; x++)
                    userId += characters[Random.Range(0, characters.Length)];
            
                var profile = new UserProfileModel
                {
                    userId = userId,
                    name = userId
                };

                userProfileController.AddUserProfileToCatalog(profile);

                var msg = new ChatMessage(ChatMessage.Type.PRIVATE, userId, $"fake message {Random.Range(0, 16000)}")
                {
                    recipient = channelId,
                    timestamp = (ulong) (fromTimestamp + i)
                };
            
                controller.AddMessageToChatWindow(JsonUtility.ToJson(msg));
            }
        }

        public void GetJoinedChannels(int limit, int skip) => GetJoinedFakeChannels(limit, skip).Forget();

        private async UniTask GetJoinedFakeChannels(int limit, int skip)
        {
            await UniTask.Delay(Random.Range(40, 1000));
            
            var characters = new[]
                {'a', 'A', 'b', 'B', 'c', 'C', 'd', 'D', 'e', 'E', 'f', 'F', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};

            var max = Mathf.Min(TotalJoinedChannelCount, skip + limit);
            
            for (var i = skip; i < max; i++)
            {
                var channelId = "";
                for (var x = 0; x < 4; x++)
                    channelId += characters[Random.Range(0, characters.Length)];

                JoinOrCreateChannel(channelId);
            }
        }

        public void GetChannels(int limit, int skip, string name) =>
            GetFakeChannels(limit, skip, name).Forget();

        private async UniTask GetFakeChannels(int limit, int skip, string name)
        {
            await UniTask.Delay(Random.Range(40, 1000));
            
            var characters = new[]
                {'a', 'A', 'b', 'B', 'c', 'C', 'd', 'D', 'e', 'E', 'f', 'F', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};
            
            for (var i = skip; i < skip + limit; i++)
            {
                var channelId = name;
                for (var x = 0; x < 4; x++)
                    channelId += characters[Random.Range(0, characters.Length)];

                var msg = new ChannelInfoPayload
                {
                    joined = true,
                    channelId = channelId,
                    muted = false,
                    memberCount = Random.Range(0, 16),
                    unseenMessages = Random.Range(0, 16)
                };
                controller.UpdateChannelInfo(JsonUtility.ToJson(msg));
            }
        }

        public void MuteChannel(string channelId) => MuteFakeChannel(channelId).Forget();

        private async UniTask MuteFakeChannel(string channelId)
        {
            await UniTask.Delay(Random.Range(40, 1000));
            
            var msg = new ChannelInfoPayload
            {
                joined = true,
                channelId = channelId,
                muted = true,
                memberCount = Random.Range(0, 16),
                unseenMessages = Random.Range(0, 16)
            };
            controller.UpdateChannelInfo(JsonUtility.ToJson(msg));
        }
    }
}