namespace MyWPF1
{
    public class ToolConfig
    {
        public string ToolKey { get; set; }           // 如 "Binary", "Enhancement"…
        public Dictionary<string, object> Params { get; set; }  // 参数名→值
    }

    public class PipelineConfig
    {
        public List<ToolConfig> Tools { get; set; } = new();
    }
}