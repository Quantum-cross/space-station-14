using System.Text.RegularExpressions;
using Content.Shared.Humanoid;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences
{
    public interface ICharacterProfile
    {
        public static readonly Regex RestrictedNameRegex = new(@"[^A-Za-z0-9 '\-]");
        public static readonly Regex ICNameCaseRegex = new(@"^(?<word>\w)|\b(?<word>\w)(?=\w*$)");

        public const int MaxNameLength = 32;

        string Name { get; protected internal set;  }

        bool Enabled { get; protected internal set; }

        ICharacterAppearance CharacterAppearance { get; }

        bool MemberwiseEquals(ICharacterProfile other);

        /// <summary>
        ///     Makes this profile valid so there's no bad data like negative ages.
        /// </summary>
        void EnsureValid(ICommonSession session, IDependencyCollection collection);

        /// <summary>
        /// Gets a copy of this profile that has <see cref="EnsureValid"/> applied, i.e. no invalid data.
        /// </summary>
        ICharacterProfile Validated(ICommonSession session, IDependencyCollection collection);

        IReadOnlySet<ProtoId<JobPrototype>> JobPreferences { get; }

        ICharacterProfile AsEnabled(bool enabledValue = true)
        {
            var ret = Clone();
            ret.Enabled = enabledValue;
            return ret;
        }

        ICharacterProfile WithCharacterAppearance(ICharacterAppearance appearance);

        ICharacterProfile Clone();

        string GetRandomName();

        ICharacterProfile WithName(string newName)
        {
            var ret = Clone();
            ret.Name = newName;
            return ret;
        }
    }
}
