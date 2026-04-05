public static class ExtendMethods
{
    public static void ParseToBuffer(this ReadOnlySpan<char> span, char separator, string[] buffer)
    {
        int colIndex = 0;
        int start = 0;
        int end;

        // 循环截取
        while (start <= span.Length)
        {
            // 查找分隔符
            end = span.Slice(start).IndexOf(separator);

            // 如果是最后一个字段
            if (end == -1) end = span.Length - start;

            // 只要文件列数不超过 buffer 长度，就不会报错
            if (colIndex < buffer.Length)
            {
                buffer[colIndex] = span.Slice(start, end).ToString();
            }

            colIndex++;
            start += end + 1;

            if (end == span.Length - start + end) break;
            if (start > span.Length) break;
        }
    }

    public static string[] SplitToStringArray(this ReadOnlySpan<char> span, char separator = '\t')
    {
        var list = new List<string>(4);
        int start = 0;
        int index;
        while ((index = span.Slice(start).IndexOf(separator)) >= 0)
        {
            list.Add(span.Slice(start, index).ToString());
            start += index + 1;
        }
        list.Add(span.Slice(start).ToString());

        return list.ToArray();
    }
}