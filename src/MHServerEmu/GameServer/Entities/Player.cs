﻿using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Achievements;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.Entities.Options;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.Missions;
using MHServerEmu.GameServer.Properties;
using MHServerEmu.GameServer.Social;

namespace MHServerEmu.GameServer.Entities
{
    public class Player : Entity
    {
        public ulong PrototypeId { get; set; }
        public Mission[] Missions { get; set; }
        public Quest[] Quests { get; set; }
        public ulong UnknownCollectionRepId { get; set;}
        public uint UnknownCollectionSize { get; set; }
        public ulong ShardId { get; set; }
        public ReplicatedString Name { get; set; }
        public ulong ConsoleAccountId1 { get; set; }
        public ulong ConsoleAccountId2 { get; set; }
        public ReplicatedString UnkName { get; set; }
        public ulong MatchQueueStatus { get; set; }
        public bool EmailVerified { get; set; }
        public ulong AccountCreationTimestamp { get; set; }
        public ulong PartyRepId { get; set; }
        public ulong PartyId { get; set; }
        public string UnknownString { get; set; }
        public bool HasGuildInfo { get; set; }
        public GuildMemberReplicationRuntimeInfo GuildInfo { get; set; }
        public bool HasCommunity { get; set; }
        public Community Community { get; set; }
        public bool UnkBool { get; set; }
        public ulong[] StashInventories { get; set; }
        public uint[] AvailableBadges { get; set; }
        public GameplayOptions GameplayOptions { get; set; }
        public AchievementState[] AchievementStates { get; set; }
        public StashTabOption[] StashTabOptions { get; set; }

        public Player(byte[] archiveData)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(archiveData);
            BoolDecoder boolDecoder = new();

            ReadEntityFields(stream);

            PrototypeId = stream.ReadPrototypeId(PrototypeEnumType.All);

            Missions = new Mission[stream.ReadRawVarint64()];
            for (int i = 0; i < Missions.Length; i++)
                Missions[i] = new(stream, boolDecoder);
            Quests = new Quest[stream.ReadRawInt32()];
            for (int i = 0; i < Quests.Length; i++)
                Quests[i] = new(stream);

            UnknownCollectionRepId = stream.ReadRawVarint64();
            UnknownCollectionSize = stream.ReadRawUInt32();

            ShardId = stream.ReadRawVarint64();
            Name = new(stream);
            ConsoleAccountId1 = stream.ReadRawVarint64();
            ConsoleAccountId2 = stream.ReadRawVarint64();
            UnkName = new(stream);
            MatchQueueStatus = stream.ReadRawVarint64();

            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            EmailVerified = boolDecoder.ReadBool();

            AccountCreationTimestamp = stream.ReadRawVarint64();

            PartyRepId = stream.ReadRawVarint64();
            PartyId = stream.ReadRawVarint64();
            
            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            HasGuildInfo = boolDecoder.ReadBool();
            if (HasGuildInfo) GuildInfo = new(stream);      // GuildMember::SerializeReplicationRuntimeInfo

            UnknownString = stream.ReadRawString();

            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            HasCommunity = boolDecoder.ReadBool();
            if (HasCommunity) Community = new(stream);

            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            UnkBool = boolDecoder.ReadBool();

            StashInventories = new ulong[stream.ReadRawVarint64()];
            for (int i = 0; i < StashInventories.Length; i++)
                StashInventories[i] = stream.ReadPrototypeId(PrototypeEnumType.All);

            AvailableBadges = new uint[stream.ReadRawVarint64()];

            GameplayOptions = new(stream, boolDecoder);

            AchievementStates = new AchievementState[stream.ReadRawVarint64()];
            for (int i = 0; i < AchievementStates.Length; i++)
                AchievementStates[i] = new(stream);

            StashTabOptions = new StashTabOption[stream.ReadRawVarint64()];
            for (int i = 0; i < StashTabOptions.Length; i++)
                StashTabOptions[i] = new(stream);
        }

        // note: this is ugly
        public Player(uint replicationPolicy, ReplicatedPropertyCollection propertyCollection,
            ulong prototypeId, Mission[] missions, Quest[] quests,
            ulong shardId, ReplicatedString playerName, ReplicatedString unkName,
            ulong matchQueueStatus, bool emailVerified, ulong accountCreationTimestamp,
            Community community, bool unkBool, ulong[] stashInventories, uint[] availableBadges,
            GameplayOptions gameplayOptions, AchievementState[] achievementStates, StashTabOption[] stashTabOptions)
        {
            ReplicationPolicy = replicationPolicy;
            PropertyCollection = propertyCollection;

            PrototypeId = prototypeId;
            Missions = missions;
            Quests = quests;
            UnknownCollectionRepId = 0;
            UnknownCollectionSize = 0;
            ShardId = shardId;
            Name = playerName;
            ConsoleAccountId1 = 0;
            ConsoleAccountId2 = 0;
            UnkName = unkName;
            MatchQueueStatus = matchQueueStatus;
            EmailVerified = emailVerified;
            AccountCreationTimestamp = accountCreationTimestamp;
            Community = community;
            UnkBool = unkBool;
            StashInventories = stashInventories;
            AvailableBadges = availableBadges;
            GameplayOptions = gameplayOptions;
            AchievementStates = achievementStates;
            StashTabOptions = stashTabOptions;
        }

        public override byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                // Prepare bool encoder
                BoolEncoder boolEncoder = new();
                byte bitBuffer;

                foreach (Mission mission in Missions)
                    boolEncoder.WriteBool(mission.BoolField);
                boolEncoder.WriteBool(EmailVerified);
                boolEncoder.WriteBool(HasGuildInfo);
                boolEncoder.WriteBool(HasCommunity);
                boolEncoder.WriteBool(UnkBool);
                foreach (ChatChannelFilter filter in GameplayOptions.ChatChannelFilters)
                    boolEncoder.WriteBool(filter.IsSubscribed);

                boolEncoder.Cook();

                // Encode
                WriteEntityFields(cos);

                cos.WritePrototypeId(PrototypeId, PrototypeEnumType.All);

                cos.WriteRawVarint64((ulong)Missions.Length);
                foreach (Mission mission in Missions)
                    cos.WriteRawBytes(mission.Encode(boolEncoder));

                cos.WriteRawInt32(Quests.Length);
                foreach (Quest quest in Quests)
                    cos.WriteRawBytes(quest.Encode());

                cos.WriteRawVarint64(UnknownCollectionRepId);
                cos.WriteRawUInt32(UnknownCollectionSize);
                cos.WriteRawVarint64(ShardId);
                cos.WriteRawBytes(Name.Encode());
                cos.WriteRawVarint64(ConsoleAccountId1);
                cos.WriteRawVarint64(ConsoleAccountId2);
                cos.WriteRawBytes(UnkName.Encode());
                cos.WriteRawVarint64(MatchQueueStatus);

                bitBuffer = boolEncoder.GetBitBuffer();             // EmailVerified
                if (bitBuffer != 0) cos.WriteRawByte(bitBuffer);

                cos.WriteRawVarint64(AccountCreationTimestamp);

                cos.WriteRawVarint64(PartyRepId);
                cos.WriteRawVarint64(PartyId);

                bitBuffer = boolEncoder.GetBitBuffer();             // HasGuildInfo
                if (bitBuffer != 0) cos.WriteRawByte(bitBuffer);

                if (HasGuildInfo) cos.WriteRawBytes(GuildInfo.Encode());

                cos.WriteRawString(UnknownString);

                bitBuffer = boolEncoder.GetBitBuffer();             // HasCommunity
                if (bitBuffer != 0) cos.WriteRawByte(bitBuffer);

                if (HasCommunity) cos.WriteRawBytes(Community.Encode());

                bitBuffer = boolEncoder.GetBitBuffer();             // UnkBool
                if (bitBuffer != 0) cos.WriteRawByte(bitBuffer);

                cos.WriteRawVarint64((ulong)StashInventories.Length);
                foreach (ulong stashInventory in StashInventories) cos.WritePrototypeId(stashInventory, PrototypeEnumType.All);

                cos.WriteRawVarint64((ulong)AvailableBadges.Length);
                foreach (uint badge in AvailableBadges)
                    cos.WriteRawVarint64(badge);

                cos.WriteRawBytes(GameplayOptions.Encode(boolEncoder));

                cos.WriteRawVarint64((ulong)AchievementStates.Length);
                foreach (AchievementState state in AchievementStates)
                    cos.WriteRawBytes(state.Encode());

                cos.WriteRawVarint64((ulong)StashTabOptions.Length);
                foreach (StashTabOption option in StashTabOptions)
                    cos.WriteRawBytes(option.Encode());

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            WriteEntityString(sb);

            sb.AppendLine($"PrototypeId: {GameDatabase.GetPrototypePath(PrototypeId)}");
            for (int i = 0; i < Missions.Length; i++) sb.AppendLine($"Mission{i}: {Missions[i]}");
            for (int i = 0; i < Quests.Length; i++) sb.AppendLine($"Quest{i}: {Quests[i]}");
            sb.AppendLine($"UnknownCollectionRepId: 0x{UnknownCollectionRepId:X}");
            sb.AppendLine($"UnknownCollectionSize: 0x{UnknownCollectionSize:X}");
            sb.AppendLine($"ShardId: {ShardId}");
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"ConsoleAccountId1: 0x{ConsoleAccountId1:X}");
            sb.AppendLine($"ConsoleAccountId2: 0x{ConsoleAccountId2:X}");
            sb.AppendLine($"UnkName: {UnkName}");
            sb.AppendLine($"MatchQueueStatus: 0x{MatchQueueStatus:X}");
            sb.AppendLine($"EmailVerified: {EmailVerified}");
            sb.AppendLine($"AccountCreationTimestamp: 0x{AccountCreationTimestamp:X}");
            sb.AppendLine($"HasGuildInfo: {HasGuildInfo}");
            sb.AppendLine($"GuildInfo: {GuildInfo}");
            sb.AppendLine($"UnknownString: {UnknownString}");
            sb.AppendLine($"HasCommunity: {HasCommunity}");
            sb.AppendLine($"Community: {Community}");
            sb.AppendLine($"UnkBool: {UnkBool}");
            for (int i = 0; i < StashInventories.Length; i++) sb.AppendLine($"StashInventory{i}: {GameDatabase.GetPrototypePath(StashInventories[i])}");
            for (int i = 0; i < AvailableBadges.Length; i++) sb.AppendLine($"AvailableBadge{i}: 0x{AvailableBadges[i]:X}");
            sb.AppendLine($"GameplayOptions: {GameplayOptions}");
            for (int i = 0; i < AchievementStates.Length; i++) sb.AppendLine($"AchievementState{i}: {AchievementStates[i]}");
            for (int i = 0; i < StashTabOptions.Length; i++) sb.AppendLine($"StashTabOption{i}: {StashTabOptions[i]}");

            return sb.ToString();
        }
    }
}
