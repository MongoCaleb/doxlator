using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace xlator
{
    public class TextBlock
    {
        [BsonElement("_id")]
        public ObjectId Id { get; set; }

        [BsonElement("source-text")]
        public string SourceText { get; set; }

        [BsonElement("translations")]
        public Dictionary<string, TranslationBlock> Translations { get; set; }

        [BsonElement("created-at")]
        public DateTimeOffset CreatedAt { get; set; }

        [BsonElement("last-modified-at")]
        public DateTimeOffset LastModifiedAt { get; set; }

        public TextBlock(DateTimeOffset? dateTimeOffset = null)
        {
            this.Translations = new Dictionary<string, TranslationBlock>();
            this.CreatedAt = dateTimeOffset ?? DateTimeOffset.UtcNow;
            this.LastModifiedAt = dateTimeOffset ?? DateTimeOffset.UtcNow;
        }
    }

    public class TranslationBlock
    {
        [BsonElement("auto")]
        public string Auto { get; set; }

        [BsonElement("manual")]
        public string Manual { get; set; }

        [BsonElement("auto-translated-at")]
        public DateTimeOffset AutoTranslatedAt { get; set; }

        [BsonElement("manually-translated-at")]
        public DateTimeOffset ManualTranslatedAt { get; set; }

        [BsonElement("manually-translated-by")]
        public string TranslatedBy { get; set; }

        public TranslationBlock(DateTimeOffset? dateTimeOffset = null)
        {
            this.AutoTranslatedAt = dateTimeOffset ?? DateTimeOffset.UtcNow;
        }
    }
}