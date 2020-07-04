
public static class TextAssetsContainer
{
    public static string rawQuestionsText;
    public static string rawNsfwQuestionsText;
    public static string rawWouldYouRatherText;
    public static string rawAnonymousNamesText;
    public static string rawFriendshipTipsText;
    public static bool isWebGl = false;

    public static void setRawQuestionsText(string s)
    {
        rawQuestionsText = s;
    }
    public static void setRawNsfwQuestionsText(string s)
    {
        rawNsfwQuestionsText = s;
    }
    public static void setRawWouldYouRatherText(string s)
    {
        rawWouldYouRatherText = s;
    }
    public static void setRawAnonymousNameText(string s)
    {
        rawAnonymousNamesText = s;
    }
    public static void setRawFriendshipTipsText(string s)
    {
        rawFriendshipTipsText = s;
    }

    public static void setIsWebGl(bool b)
    {
        isWebGl = b;
    }
}
