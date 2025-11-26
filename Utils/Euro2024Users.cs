public static class Euro2024Users
{
    private static readonly Dictionary<string, string> users = new Dictionary<string, string>
    {
        { "27c304b4-f3be-4beb-9833-a54a7f6deaab", "Hermund Myklemyr" },
        { "38c867aa-e372-46ca-9a1c-4d85623f56f3", "Adrian Vigdal" },
        { "3b9fc15f-ad93-4e1e-b891-29f0a175c3bb", "Kenneth Heggestad" },
        { "4214cb72-756e-4397-a555-394019cd4826", "Tore Madsen" },
        { "5b94ed89-e8e8-409d-8144-69ab4ceb4c76", "Bernt A Elle" },
        { "68ac9525-e13f-4d60-8b04-22afacfe79d6", "Jo Bukve" },
        { "7ccede61-445c-4328-bd2d-597cb2b36375", "Bjørn Tore Årøy" },
        { "81e706f3-652e-4174-888a-1346f5043212", "Ørjan Hestetun" },
        { "8c374c71-9dd2-482e-add9-09d7e305f0a7", "Frode Kvalsøren" },
        { "a3298f3b-4695-43e6-8d7c-b8d6648857e4", "Lars Jørgen Kjærvik" },
        { "b887c8b9-2246-41a3-8563-d65f35f6864a", "Thomas Ness" },
        { "c11b0977-2633-4a9a-94fa-740d6fabddd4", "Jarle Nes" },
        { "efc86ff4-824d-464a-bf00-f0b7285f5bc4", "Endre Westgaard" },
        { "aa19758f-479b-4105-8e4b-87d1f5bd4775", "Tone Berge" },
        {"8f477990-e3d8-41e4-b67e-5f3185034ec8", "Adrian H Vigdal"},
        {"34b0c68b-9eee-4041-9b81-d303de86ec89", "Adri"}
    };

    public static string HentBrukernavn(string brukerId)
    {
        if (users.TryGetValue(brukerId, out string brukernavn))
        {
            return brukernavn;
        }
        else
        {
            return "Anonyme Anton";
        }
    }

    public static string HentKortBrukernavn(string brukerId)
    {
        var userName = HentBrukernavn(brukerId);
        return GetTwoLetterByName(userName);
    }

    public static string HentEtternavn(string brukerId)
    {
        var userName = HentBrukernavn(brukerId);
        return GetLastNameByName(userName);
    }

    private static string GetTwoLetterByName(string name)
    {
        string[] names = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (names.Length == 0)
        {
            return string.Empty; // Return an empty string if no names are provided
        }

        string firstNameInitial = names[0][0].ToString().ToUpper();
        string lastNameInitial = names[^1][0].ToString().ToUpper(); // ^1 is the last element

        return $"{firstNameInitial}{lastNameInitial}";
    }
    private static string GetLastNameByName(string name)
    {
        string[] names = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (names.Length == 0)
        {
            return string.Empty; // Return an empty string if no names are provided
        }

        return names[^1];
    }
}