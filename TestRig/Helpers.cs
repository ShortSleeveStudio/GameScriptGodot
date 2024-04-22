using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace GameScript
{
    public static class Helpers
    {
        public static Dictionary<string, bool> S = new();

        public static async void TestLease(Lease lease, int millis = 1000)
        {
            GD.Print($"Waiting {millis} milliseconds");
            await Task.Delay(millis);
            GD.Print("Done waiting, let's do this!");
            lease.Release();
        }

        public static void TestNode(Node currentNode)
        {
            GD.Print($"Current Node: {currentNode.Id}");
        }

        public static void PrintProperties(Node currentNode)
        {
            if (currentNode.Properties != null)
            {
                for (int i = 0; i < currentNode.Properties.Length; i++)
                {
                    Property prop = currentNode.Properties[i];
                    switch (prop)
                    {
                        case StringProperty stringProperty:
                            GD.Print(stringProperty.GetString());
                            break;
                        case IntegerProperty integerProperty:
                            GD.Print(integerProperty.GetInteger());
                            break;
                        case DecimalProperty decimalProperty:
                            GD.Print(decimalProperty.GetDecimal());
                            break;
                        case BooleanProperty booleanProperty:
                            GD.Print(booleanProperty.GetBoolean());
                            break;
                        case EmptyProperty:
                            GD.Print("empty");
                            break;
                        default:
                            GD.Print("Unknown Prop type: " + prop);
                            break;
                    }
                }
            }
        }
    }
}
