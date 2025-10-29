namespace Tlumach.Base
{
    public class TranslationTreeNode
    {
        public Dictionary<string, TranslationTreeNode> ChildNodes { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<string> Keys { get; } = [];

        public string Name { get; }

        public TranslationTreeNode(string name)
        {
            Name = name;
        }

        public TranslationTreeNode? FindNode(string name)
        {
            TranslationTreeNode? result = null;
            int idx = name.IndexOf('.');
            if (idx == -1)
            {
                if (ChildNodes.TryGetValue(name, out result))
                    return result;
            }
            else
            if (idx > 0)
            {
                if (ChildNodes.TryGetValue(name.Substring(0, idx), out result) && result is not null)
                {
                    return result.FindNode(name.Substring(idx + 1));
                }
            }
            return null;
        }

        public TranslationTreeNode? MakeNode(string name)
        {
            TranslationTreeNode? result = null;
            int idx = name.IndexOf('.');
            if (idx == -1)
            {
                if (ChildNodes.TryGetValue(name, out result))
                    return result;
                result = new TranslationTreeNode(name);
                ChildNodes.Add(name, result);
                return result;
            }
            else
            if (idx > 0)
            {
                string ownName = name.Substring(0, idx);
                string subName = name.Substring(idx + 1);
                if (ChildNodes.TryGetValue(ownName, out result) && result is not null)
                {
                    return result.MakeNode(subName);
                }

                result = new TranslationTreeNode(ownName);
                ChildNodes.Add(ownName, result);
                return result.MakeNode(subName);
            }
            else
                return null;
        }
    }
}
