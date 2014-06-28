#region usings
using System;
using System.ComponentModel.Composition;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "DecodePolyline", Category = "Polyline", Help = "Basic template with one value in/out", Tags = "")]
	#endregion PluginInfo
	public class PolylineDecodePolylineNode : IPluginEvaluate
	{
		#region fields & pins
        [Input("Polyline", DefaultString = "")]
		public ISpread<string> FPolyline;

		[Output("Latitude")]
        public ISpread<double> FLatitude;

        [Output("Longtitude")]
        public ISpread<double> FLongitude;

        [Output("BinSize")]
        public ISpread<double> FBinSize;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
        {
           
            List<double> lat = new List<double>();
            List<double> lon = new List<double>();
            List<double> bin = new List<double>();
            int index = 0;
            try
            {
                for (int i = 0; i < SpreadMax; i++)
                {
                    var res = DecodePolylinePoints(FPolyline[i]);
                    index = index + res.Count;
                    bin.Add(res.Count);
                    for (int j = 0; j < res.Count; j++)
                    {
                        lat.Add(res[j].Latitude);
                        lon.Add(res[j].Longitude);
                    }

                }
                FLatitude.SliceCount = index;
                FLongitude.SliceCount = index;
                FBinSize.SliceCount = SpreadMax;
                for (int i = 0; i < bin.Count; i++)
                    FBinSize[i] = bin[i];
                for (int i = 0; i < index; i++)
                {
                    FLatitude[i] = lat[i];
                    FLongitude[i] = lon[i];
                }
            }
            catch
            {

            }
		}
        private List<Location> DecodePolylinePoints(string encodedPoints)
        {
            if (encodedPoints == null || encodedPoints == "") return null;
            List<Location> poly = new List<Location>();
            char[] polylinechars = encodedPoints.ToCharArray();
            int index = 0;

            int currentLat = 0;
            int currentLng = 0;
            int next5bits;
            int sum;
            int shifter;

            try
            {
                while (index < polylinechars.Length)
                {
                    // calculate next latitude
                    sum = 0;
                    shifter = 0;
                    do
                    {
                        next5bits = (int)polylinechars[index++] - 63;
                        sum |= (next5bits & 31) << shifter;
                        shifter += 5;
                    } while (next5bits >= 32 && index < polylinechars.Length);

                    if (index >= polylinechars.Length)
                        break;

                    currentLat += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                    //calculate next longitude
                    sum = 0;
                    shifter = 0;
                    do
                    {
                        next5bits = (int)polylinechars[index++] - 63;
                        sum |= (next5bits & 31) << shifter;
                        shifter += 5;
                    } while (next5bits >= 32 && index < polylinechars.Length);

                    if (index >= polylinechars.Length && next5bits >= 32)
                        break;

                    currentLng += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);
                    Location p = new Location();
                    p.Latitude = Convert.ToDouble(currentLat) / 100000.0;
                    p.Longitude = Convert.ToDouble(currentLng) / 100000.0;
                    poly.Add(p);
                }
            }
            catch (Exception ex)
            {
                // logo it
            }
            return poly;
        }
	}
    public struct Location
    {
        public double Latitude;
        public double Longitude;
    }
}
