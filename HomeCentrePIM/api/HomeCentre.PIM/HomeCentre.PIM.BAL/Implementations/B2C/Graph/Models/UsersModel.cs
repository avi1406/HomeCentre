// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace HomeCentre.PIM.BAL.Implementations.B2C.Graph.Models
{
    public class UsersModel
    {
        public UserModel[] Users { get; set; }

        public static UsersModel Parse(string json)
        {
            return JsonConvert.DeserializeObject(json, typeof(UsersModel)) as UsersModel;
        }
    }
}