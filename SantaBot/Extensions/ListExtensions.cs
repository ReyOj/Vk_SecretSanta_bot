namespace SantaBot.Extensions;

public static class ListExtensions
{
    private static readonly Random Rnd = new();
    
    public static List<T> Shuffle<T>(this List<T> array)
    {
        var newArray = new T[array.Count];

        for (var i = 0; i < newArray.Length; i++)
        {
            int newId;
            do
            {
                newId = Rnd.Next(0, newArray.Length);
            } while (newId == i || (newArray[newId] is not null && !newArray[newId]!.Equals(default(T))));

            newArray[newId] = array[i];
        }

        return newArray.ToList();
    }
}