using System;
using System.Collections.Generic;
using UnityEngine;

 [Serializable]
public sealed class AvatarDefinition
{
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite sprite;
        public string Id => id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? id : displayName;
        public Sprite Sprite => sprite;
}

 [CreateAssetMenu(menuName = "Capivarada/Profile/Avatar Catalog", fileName = "AvatarCatalog")]
public sealed class AvatarCatalog : ScriptableObject
{
        [SerializeField] private List<AvatarDefinition> avatars = new List<AvatarDefinition>();
        public IReadOnlyList<AvatarDefinition> Avatars => avatars;
        public AvatarDefinition Find(string id) => avatars.Find(avatar => avatar != null && avatar.Id == id);
        public AvatarDefinition Default => avatars.Count > 0 ? avatars[0] : null;
}
