using DCLServices.Lambdas;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DCLServices.Lambdas.PlaceService
{
    public class PlacesResponse : PaginatedResponse
    {

        public bool ok;
        public int total;
        public List<PlaceInfo> data;

        [Serializable]
        public class PlaceInfo
        {
            [Serializable]
            public class Realm
            {
                public string serverName;
                public string layer;
                public string url;
                public int usersCount;
                public int maxUsers;
                public Vector2Int[] userParcels;
            }

            public string id;
            public string title;
            public string description;
            public string image;
            public string owner;
            public string[] tags;
            public string[] positions;
            public string base_position;
            public string contact_name;
            public string contact_email;
            public string content_rating;
            public bool disabled;
            public string disabled_at;
            public string created_at;
            public string updated_at;
            public int favorites;
            public int likes;
            public int dislikes;
            public string[] categories;
            public int like_rate;
            public bool highlighted;
            public string highlighted_image;
            public bool featured;
            public string featured_image;
            public bool user_favorite;
            public bool user_like;
            public bool user_dislike;
            public int user_count;
            public int user_visits;
            public Realm[] realms_detail;
        }
    }
}
