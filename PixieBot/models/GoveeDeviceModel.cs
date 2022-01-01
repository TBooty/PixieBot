using System;
using System.Collections.Generic;
using System.Text;

namespace PixieBot.modules
{
    public class Range
    {
        public int min { get; set; }
        public int max { get; set; }
    }

    public class ColorTem
    {
        public Range range { get; set; }
    }

    public class Properties
    {
        public ColorTem colorTem { get; set; }
    }

    public class Device
    {
        public string device { get; set; }
        public string model { get; set; }
        public string deviceName { get; set; }
        public bool controllable { get; set; }
        public bool retrievable { get; set; }
        public List<string> supportCmds { get; set; }
        public Properties properties { get; set; }
    }


    public class Cmd
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class GoveeRequest
    {
        public string device { get; set; }
        public string model { get; set; }
        public Cmd cmd { get; set; }
    }


}
