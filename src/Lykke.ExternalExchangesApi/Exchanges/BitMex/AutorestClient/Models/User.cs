// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

using Microsoft.Rest;
using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.BitMex.AutorestClient.Models
{
    /// <summary>
    /// Account Operations
    /// </summary>
    public partial class User
    {
        /// <summary>
        /// Initializes a new instance of the User class.
        /// </summary>
        public User()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the User class.
        /// </summary>
        public User(string username, string email, double? id = default(double?), double? ownerId = default(double?), string firstname = default(string), string lastname = default(string), string phone = default(string), System.DateTime? created = default(System.DateTime?), System.DateTime? lastUpdated = default(System.DateTime?), UserPreferences preferences = default(UserPreferences), string tFAEnabled = default(string), string affiliateID = default(string), string pgpPubKey = default(string), string country = default(string))
        {
            Id = id;
            OwnerId = ownerId;
            Firstname = firstname;
            Lastname = lastname;
            Username = username;
            Email = email;
            Phone = phone;
            Created = created;
            LastUpdated = lastUpdated;
            Preferences = preferences;
            TFAEnabled = tFAEnabled;
            AffiliateID = affiliateID;
            PgpPubKey = pgpPubKey;
            Country = country;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public double? Id { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "ownerId")]
        public double? OwnerId { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "firstname")]
        public string Firstname { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "lastname")]
        public string Lastname { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "username")]
        public string Username { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "phone")]
        public string Phone { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "created")]
        public System.DateTime? Created { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "lastUpdated")]
        public System.DateTime? LastUpdated { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "preferences")]
        public UserPreferences Preferences { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "TFAEnabled")]
        public string TFAEnabled { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "affiliateID")]
        public string AffiliateID { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "pgpPubKey")]
        public string PgpPubKey { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "country")]
        public string Country { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            if (Username == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Username");
            }
            if (Email == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Email");
            }
            if (AffiliateID != null)
            {
                if (AffiliateID.Length > 6)
                {
                    throw new ValidationException(ValidationRules.MaxLength, "AffiliateID", 6);
                }
            }
            if (PgpPubKey != null)
            {
                if (PgpPubKey.Length > 16384)
                {
                    throw new ValidationException(ValidationRules.MaxLength, "PgpPubKey", 16384);
                }
            }
            if (Country != null)
            {
                if (Country.Length > 3)
                {
                    throw new ValidationException(ValidationRules.MaxLength, "Country", 3);
                }
            }
        }
    }
}
