// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.BitMex.AutorestClient.Models
{
    public partial class UserPreferences
    {
        /// <summary>
        /// Initializes a new instance of the UserPreferences class.
        /// </summary>
        public UserPreferences()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the UserPreferences class.
        /// </summary>
        public UserPreferences(bool? alertOnLiquidations = default(bool?), bool? animationsEnabled = default(bool?), System.DateTime? announcementsLastSeen = default(System.DateTime?), double? chatChannelID = default(double?), string colorTheme = default(string), string currency = default(string), bool? debug = default(bool?), IList<string> disableEmails = default(IList<string>), IList<string> hideConfirmDialogs = default(IList<string>), bool? hideConnectionModal = default(bool?), bool? hideFromLeaderboard = default(bool?), bool? hideNameFromLeaderboard = default(bool?), IList<string> hideNotifications = default(IList<string>), string locale = default(string), IList<string> msgsSeen = default(IList<string>), object orderBookBinning = default(object), string orderBookType = default(string), bool? orderClearImmediate = default(bool?), bool? orderControlsPlusMinus = default(bool?), IList<string> sounds = default(IList<string>), bool? strictIPCheck = default(bool?), bool? strictTimeout = default(bool?), string tickerGroup = default(string), bool? tickerPinned = default(bool?), string tradeLayout = default(string))
        {
            AlertOnLiquidations = alertOnLiquidations;
            AnimationsEnabled = animationsEnabled;
            AnnouncementsLastSeen = announcementsLastSeen;
            ChatChannelID = chatChannelID;
            ColorTheme = colorTheme;
            Currency = currency;
            Debug = debug;
            DisableEmails = disableEmails;
            HideConfirmDialogs = hideConfirmDialogs;
            HideConnectionModal = hideConnectionModal;
            HideFromLeaderboard = hideFromLeaderboard;
            HideNameFromLeaderboard = hideNameFromLeaderboard;
            HideNotifications = hideNotifications;
            Locale = locale;
            MsgsSeen = msgsSeen;
            OrderBookBinning = orderBookBinning;
            OrderBookType = orderBookType;
            OrderClearImmediate = orderClearImmediate;
            OrderControlsPlusMinus = orderControlsPlusMinus;
            Sounds = sounds;
            StrictIPCheck = strictIPCheck;
            StrictTimeout = strictTimeout;
            TickerGroup = tickerGroup;
            TickerPinned = tickerPinned;
            TradeLayout = tradeLayout;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "alertOnLiquidations")]
        public bool? AlertOnLiquidations { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "animationsEnabled")]
        public bool? AnimationsEnabled { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "announcementsLastSeen")]
        public System.DateTime? AnnouncementsLastSeen { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "chatChannelID")]
        public double? ChatChannelID { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "colorTheme")]
        public string ColorTheme { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "currency")]
        public string Currency { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "debug")]
        public bool? Debug { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "disableEmails")]
        public IList<string> DisableEmails { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "hideConfirmDialogs")]
        public IList<string> HideConfirmDialogs { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "hideConnectionModal")]
        public bool? HideConnectionModal { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "hideFromLeaderboard")]
        public bool? HideFromLeaderboard { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "hideNameFromLeaderboard")]
        public bool? HideNameFromLeaderboard { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "hideNotifications")]
        public IList<string> HideNotifications { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "locale")]
        public string Locale { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "msgsSeen")]
        public IList<string> MsgsSeen { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "orderBookBinning")]
        public object OrderBookBinning { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "orderBookType")]
        public string OrderBookType { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "orderClearImmediate")]
        public bool? OrderClearImmediate { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "orderControlsPlusMinus")]
        public bool? OrderControlsPlusMinus { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "sounds")]
        public IList<string> Sounds { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "strictIPCheck")]
        public bool? StrictIPCheck { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "strictTimeout")]
        public bool? StrictTimeout { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "tickerGroup")]
        public string TickerGroup { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "tickerPinned")]
        public bool? TickerPinned { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "tradeLayout")]
        public string TradeLayout { get; set; }

    }
}
