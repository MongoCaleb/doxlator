using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace xlator
{
    public class SourceDocument
    {
        [BsonIgnoreIfNull]
        public ObjectId _id { get; set; }
        [BsonIgnoreIfNull]
        public string page_id { get; set; }
        [BsonIgnoreIfNull]
        public object prefix { get; set; }
        [BsonIgnoreIfNull]
        public string filename { get; set; }
        [BsonIgnoreIfNull]
        public string source { get; set; }
        [BsonIgnoreIfNull]
        public DateTime created_at { get; set; }
        [BsonIgnoreIfNull]
        public List<StaticAsset> static_assets { get; set; }
        [BsonIgnoreIfNull]
        public Ast ast { get; set; }

        public SourceDocument()
        {
            this.static_assets = new List<StaticAsset>();
        }
    }
    [BsonNoId]
    public class Ast
    {
        [BsonIgnoreIfNull]
        [BsonElement("id")]
        public string id { get; set; }
        [BsonIgnoreIfNull]
        public string type { get; set; }
        [BsonIgnoreIfNull]
        public object position { get; set; } //TODO
        [BsonIgnoreIfNull]
        public object fileid { get; set; }
        [BsonIgnoreIfNull]
        public object options { get; set; } //todo
        [BsonIgnoreIfNull]
        public string value { get; set; }
        [BsonIgnoreIfNull]
        public List<Ast> children { get; set; }
        [BsonIgnoreIfNull]
        public object ids { get; set; }
        [BsonIgnoreIfNull]
        public object domain { get; set; }
        [BsonIgnoreIfNull]
        public object name { get; set; }
        [BsonIgnoreIfNull]
        public object html_id { get; set; }
        [BsonIgnoreIfNull]
        public object argument { get; set; }
        [BsonIgnoreIfNull]
        public object entries { get; set; }
        [BsonIgnoreIfNull]
        public object enumtype { get; set; }
        [BsonIgnoreIfNull]
        public object target { get; set; }
        [BsonIgnoreIfNull]
        public object flag { get; set; }
        [BsonIgnoreIfNull]
        public object refuri { get; set; }
        [BsonIgnoreIfNull]
        public object refname { get; set; }
        [BsonIgnoreIfNull]
        public object lang { get; set; }
        [BsonIgnoreIfNull]
        public object url { get; set; }
        [BsonIgnoreIfNull]
        public object copyable { get; set; }
        [BsonIgnoreIfNull]
        public object emphasize_lines { get; set; }
        [BsonIgnoreIfNull]
        public object linenos { get; set; }

        /* [BsonExtraElements]
         public BsonDocument CatchAll { get; set; }
        */
        public Ast()
        {
            this.children = new List<Ast>();
        }

    }
    public class StaticAsset
    {
        [BsonIgnoreIfNull]
        public string checksum { get; set; }
        [BsonIgnoreIfNull]
        public string key { get; set; }

    }
}




