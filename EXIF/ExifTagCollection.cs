using System.Collections;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace Ion.Windows;

public sealed class ExifTagCollection : IEnumerable<ExifTag>
{
    private Dictionary<int, ExifTag> tags;

    #region ExifTagCollection

    public ExifTag this[int id] => tags[id];

    public ExifTagCollection(string filePath) : this(filePath, true, false) { }

    public ExifTagCollection(string filePath, bool useEmbeddedColorManagement, bool validateImageData)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            System.Drawing.Image image = System.Drawing.Image.FromStream(stream, useEmbeddedColorManagement, validateImageData);
            ReadTags(image.PropertyItems);
        }
        catch { }
    }

    public ExifTagCollection(System.Drawing.Image image) => ReadTags(image.PropertyItems);

    #endregion

    #region Methods

    private static string GetComponentsConfig(byte[] bytes)
    {
        string s = "";
        string[] vals = ["", "Y", "Cb", "Cr", "R", "G", "B"];

        foreach (byte b in bytes)
            s += vals[b];

        return s;
    }

    private void ReadTags(PropertyItem[] pitems)
    {
        Encoding ascii = Encoding.ASCII;
        tags = [];

        foreach (DictionaryEntry Entry in ExifHelper.Tags)
        {
            ExifTag TagToAdd = (ExifTag)Entry.Value;
            string value = "";
            foreach (PropertyItem pitem in pitems)
            {
                ExifTag TagToCheck = (ExifTag)ExifHelper.Tags[pitem.Id];
                if (TagToCheck is null) continue;
                if (TagToCheck.Id != TagToAdd.Id) continue;
                if (pitem.Type == 0x1)
                {
                    #region BYTE (8-bit unsigned int)
                    if (pitem.Value.Length == 4)
                        value = "Version " + pitem.Value[0].ToString() + "." + pitem.Value[1].ToString();
                    else if (pitem.Id == 0x5 && pitem.Value[0] == 0)
                        value = "Sea level";
                    else
                        value = pitem.Value[0].ToString();
                    #endregion
                }
                else if (pitem.Type == 0x2)
                {
                    #region ASCII (8 bit ASCII code)

                    value = ascii.GetString(pitem.Value).Trim('\0');

                    if (pitem.Id == 0x1 || pitem.Id == 0x13)
                        if (value == "N") value = "North latitude";
                        else if (value == "S") value = "South latitude";
                        else value = "reserved";

                    if (pitem.Id == 0x3 || pitem.Id == 0x15)
                        if (value == "E") value = "East longitude";
                        else if (value == "W") value = "West longitude";
                        else value = "reserved";

                    if (pitem.Id == 0x9)
                        if (value == "A") value = "Measurement in progress";
                        else if (value == "V") value = "Measurement Interoperability";
                        else value = "reserved";

                    if (pitem.Id == 0xA)
                        if (value == "2") value = "2-dimensional measurement";
                        else if (value == "3") value = "3-dimensional measurement";
                        else value = "reserved";

                    if (pitem.Id == 0xC || pitem.Id == 0x19)
                        if (value == "K") value = "Kilometers per hour";
                        else if (value == "M") value = "Miles per hour";
                        else if (value == "N") value = "Knots";
                        else value = "reserved";

                    if (pitem.Id == 0xE || pitem.Id == 0x10 || pitem.Id == 0x17)
                        if (value == "T") value = "True direction";
                        else if (value == "M") value = "Magnetic direction";
                        else value = "reserved";
                    #endregion
                }
                else if (pitem.Type == 0x3)
                {
                    #region 3 = SHORT (16-bit unsigned int)

                    ushort uintval = BitConverter.ToUInt16(pitem.Value, 0);

                    // orientation // lookup table					
                    switch (pitem.Id)
                    {
                        case 0x8827: // ISO speed rating
                            value = "ISO-" + uintval.ToString();
                            break;
                        case 0xA217: // sensing method
                        {
                            value = uintval switch
                            {
                                1 => "Not defined",
                                2 => "One-chip color area sensor",
                                3 => "Two-chip color area sensor",
                                4 => "Three-chip color area sensor",
                                5 => "Color sequential area sensor",
                                7 => "Trilinear sensor",
                                8 => "Color sequential linear sensor",
                                _ => " reserved",
                            };
                        }
                        break;
                        case 0x8822: // Exposure program
                            value = uintval switch
                            {
                                0 => "Not defined",
                                1 => "Manual",
                                2 => "Normal program",
                                3 => "Aperture priority",
                                4 => "Shutter priority",
                                5 => "Creative program (biased toward depth of field)",
                                6 => "Action program (biased toward fast shutter speed)",
                                7 => "Portrait mode (for closeup photos with the background out of focus)",
                                8 => "Landscape mode (for landscape photos with the background in focus)",
                                _ => "reserved",
                            };
                            break;
                        case 0x9207: // metering mode
                            value = uintval switch
                            {
                                0 => "unknown",
                                1 => "Average",
                                2 => "Center Weighted Average",
                                3 => "Spot",
                                4 => "MultiSpot",
                                5 => "Pattern",
                                6 => "Partial",
                                255 => "Other",
                                _ => "reserved",
                            };
                            break;
                        case 0x9208: // Light source
                        {
                            value = uintval switch
                            {
                                0 => "unknown",
                                1 => "Daylight",
                                2 => "Fluorescent",
                                3 => "Tungsten (incandescent light)",
                                4 => "Flash",
                                9 => "Fine weather",
                                10 => "Cloudy weather",
                                11 => "Shade",
                                12 => "Daylight fluorescent (D 5700 – 7100K)",
                                13 => "Day white fluorescent (N 4600 – 5400K)",
                                14 => "Cool white fluorescent (W 3900 – 4500K)",
                                15 => "White fluorescent (WW 3200 – 3700K)",
                                17 => "Standard light A",
                                18 => "Standard light B",
                                19 => "Standard light C",
                                20 => "D55",
                                21 => "D65",
                                22 => "D75",
                                23 => "D50",
                                24 => "ISO studio tungsten",
                                255 => "ISO studio tungsten",
                                _ => "other light source",
                            };
                        }
                        break;
                        case 0x9209: // Flash
                        {
                            value = uintval switch
                            {
                                0x0 => "Flash did not fire",
                                0x1 => "Flash fired",
                                0x5 => "Strobe return light not detected",
                                0x7 => "Strobe return light detected",
                                0x9 => "Flash fired, compulsory flash mode",
                                0xD => "Flash fired, compulsory flash mode, return light not detected",
                                0xF => "Flash fired, compulsory flash mode, return light detected",
                                0x10 => "Flash did not fire, compulsory flash mode",
                                0x18 => "Flash did not fire, auto mode",
                                0x19 => "Flash fired, auto mode",
                                0x1D => "Flash fired, auto mode, return light not detected",
                                0x1F => "Flash fired, auto mode, return light detected",
                                0x20 => "No flash function",
                                0x41 => "Flash fired, red-eye reduction mode",
                                0x45 => "Flash fired, red-eye reduction mode, return light not detected",
                                0x47 => "Flash fired, red-eye reduction mode, return light detected",
                                0x49 => "Flash fired, compulsory flash mode, red-eye reduction mode",
                                0x4D => "Flash fired, compulsory flash mode, red-eye reduction mode, return light not detected",
                                0x4F => "Flash fired, compulsory flash mode, red-eye reduction mode, return light detected",
                                0x59 => "Flash fired, auto mode, red-eye reduction mode",
                                0x5D => "Flash fired, auto mode, return light not detected, red-eye reduction mode",
                                0x5F => "Flash fired, auto mode, return light detected, red-eye reduction mode",
                                _ => "reserved",
                            };
                        }
                        break;
                        case 0x0128: //ResolutionUnit
                        {
                            value = uintval switch
                            {
                                2 => "Inch",
                                3 => "Centimeter",
                                _ => "No Unit",
                            };
                        }
                        break;
                        case 0xA409: // Saturation
                        {
                            value = uintval switch
                            {
                                0 => "Normal",
                                1 => "Low saturation",
                                2 => "High saturation",
                                _ => "Reserved",
                            };
                        }
                        break;

                        case 0xA40A: // Sharpness
                        {
                            value = uintval switch
                            {
                                0 => "Normal",
                                1 => "Soft",
                                2 => "Hard",
                                _ => "Reserved",
                            };
                        }
                        break;
                        case 0xA408: // Contrast
                        {
                            value = uintval switch
                            {
                                0 => "Normal",
                                1 => "Soft",
                                2 => "Hard",
                                _ => "Reserved",
                            };
                        }
                        break;
                        case 0x103: // Compression
                        {
                            value = uintval switch
                            {
                                1 => "Uncompressed",
                                6 => "JPEG compression (thumbnails only)",
                                _ => "Reserved",
                            };
                        }
                        break;
                        case 0x106: // PhotometricInterpretation
                        {
                            value = uintval switch
                            {
                                2 => "RGB",
                                6 => "YCbCr",
                                _ => "Reserved",
                            };
                        }
                        break;
                        case 0x112: // Orientation
                        {
                            value = uintval switch
                            {
                                1 => "The 0th row is at the visual top of the image, and the 0th column is the visual left-hand side.",
                                2 => "The 0th row is at the visual top of the image, and the 0th column is the visual right-hand side.",
                                3 => "The 0th row is at the visual bottom of the image, and the 0th column is the visual right-hand side.",
                                4 => "The 0th row is at the visual bottom of the image, and the 0th column is the visual left-hand side.",
                                5 => "The 0th row is the visual left-hand side of the image, and the 0th column is the visual top.",
                                6 => "The 0th row is the visual right-hand side of the image, and the 0th column is the visual top.",
                                7 => "The 0th row is the visual right-hand side of the image, and the 0th column is the visual bottom.",
                                8 => "The 0th row is the visual left-hand side of the image, and the 0th column is the visual bottom.",
                                _ => "Reserved",
                            };
                        }
                        break;
                        case 0x213: // YCbCrPositioning
                        {
                            value = uintval switch
                            {
                                1 => "centered",
                                6 => "co-sited",
                                _ => "Reserved",
                            };
                        }
                        break;
                        case 0xA001: // ColorSpace
                        {
                            value = uintval switch
                            {
                                1 => "sRGB",
                                0xFFFF => "Uncalibrated",
                                _ => "Reserved",
                            };
                        }
                        break;
                        case 0xA401: // CustomRendered
                        {
                            value = uintval switch
                            {
                                0 => "Normal process",
                                1 => "Custom process",
                                _ => "Reserved",
                            };
                        }
                        break;
                        case 0xA402: // ExposureMode
                        {
                            value = uintval switch
                            {
                                0 => "Auto exposure",
                                1 => "Manual exposure",
                                2 => "Auto bracket",
                                _ => "Reserved",
                            };
                        }
                        break;
                        case 0xA403: // WhiteBalance
                        {
                            value = uintval switch
                            {
                                0 => "Auto white balance",
                                1 => "Manual white balance",
                                _ => "Reserved",
                            };
                        }
                        break;
                        case 0xA406: // SceneCaptureType
                        {
                            value = uintval switch
                            {
                                0 => "Standard",
                                1 => "Landscape",
                                2 => "Portrait",
                                3 => "Night scene",
                                _ => "Reserved",
                            };
                        }
                        break;

                        case 0xA40C: // SubjectDistanceRange
                        {
                            value = uintval switch
                            {
                                0 => "unknown",
                                1 => "Macro",
                                2 => "Close view",
                                3 => "Distant view",
                                _ => "Reserved",
                            };
                        }
                        break;
                        case 0x1E: // GPSDifferential
                        {
                            value = uintval switch
                            {
                                0 => "Measurement without differential correction",
                                1 => "Differential correction applied",
                                _ => "Reserved",
                            };
                        }
                        break;
                        case 0xA405: // FocalLengthIn35mmFilm
                            value = uintval.ToString() + " mm";
                            break;
                        default://
                            value = uintval.ToString();
                            break;
                    }
                    #endregion
                }
                else if (pitem.Type == 0x4)
                {
                    #region 4 = LONG (32-bit unsigned int)
                    value = BitConverter.ToUInt32(pitem.Value, 0).ToString();
                    #endregion
                }
                else if (pitem.Type == 0x5)
                {
                    #region 5 = RATIONAL (Two LONGs, unsigned)

                    ExifHelper.GPSRational rat = new(pitem.Value);

                    switch (pitem.Id)
                    {
                        case 0x9202: // ApertureValue
                            value = "F/" + Math.Round(Math.Pow(Math.Sqrt(2), Convert.ToDouble(rat)), 2).ToString();
                            break;
                        case 0x9205: // MaxApertureValue
                            value = "F/" + Math.Round(Math.Pow(Math.Sqrt(2), Convert.ToDouble(rat)), 2).ToString();
                            break;
                        case 0x920A: // FocalLength
                            value = Convert.ToDouble(rat).ToString() + " mm";
                            break;
                        case 0x829D: // F-number
                            value = "F/" + Convert.ToDouble(rat).ToString();
                            break;
                        case 0x11A: // Xresolution
                            value = Convert.ToDouble(rat).ToString();
                            break;
                        case 0x11B: // Yresolution
                            value = Convert.ToDouble(rat).ToString();
                            break;
                        case 0x829A: // ExposureTime
                            value = rat.ToString() + " sec";
                            break;
                        case 0x2: // GPSLatitude                                
                            value = Convert.ToDecimal(new ExifHelper.GPSRational(pitem.Value)).ToString();
                            break;
                        case 0x4: // GPSLongitude
                            value = Convert.ToDecimal(new ExifHelper.GPSRational(pitem.Value)).ToString();
                            break;
                        case 0x6: // GPSAltitude
                            value = Convert.ToDouble(rat) + " meters";
                            break;
                        case 0xA404: // Digital Zoom Ratio
                            value = Convert.ToDouble(rat).ToString();
                            if (value == "0") value = "none";
                            break;
                        case 0xB: // GPSDOP
                            value = Convert.ToDouble(rat).ToString();
                            break;
                        case 0xD: // GPSSpeed
                            value = Convert.ToDouble(rat).ToString();
                            break;
                        case 0xF: // GPSTrack
                            value = Convert.ToDouble(rat).ToString();
                            break;
                        case 0x11: // GPSImgDir
                            value = Convert.ToDouble(rat).ToString();
                            break;
                        case 0x14: // GPSDestLatitude
                            value = new ExifHelper.GPSRational(pitem.Value).ToString();
                            break;
                        case 0x16: // GPSDestLongitude
                            value = new ExifHelper.GPSRational(pitem.Value).ToString();
                            break;
                        case 0x18: // GPSDestBearing
                            value = Convert.ToDouble(rat).ToString();
                            break;
                        case 0x1A: // GPSDestDistance
                            value = Convert.ToDouble(rat).ToString();
                            break;
                        case 0x7: // GPSTimeStamp                                
                            value = new ExifHelper.GPSRational(pitem.Value).ToString();
                            break;

                        default:
                            value = rat.ToString();
                            break;
                    }

                    #endregion
                }
                else if (pitem.Type == 0x7)
                {
                    #region UNDEFINED (8-bit)
                    switch (pitem.Id)
                    {
                        case 0xA300: //FileSource
                        {
                            if (pitem.Value[0] == 3)
                                value = "DSC";
                            else
                                value = "reserved";
                            break;
                        }
                        case 0xA301: //SceneType
                            if (pitem.Value[0] == 1)
                                value = "A directly photographed image";
                            else
                                value = "reserved";
                            break;
                        case 0x9000:// Exif Version
                            value = ascii.GetString(pitem.Value).Trim('\0');
                            break;
                        case 0xA000: // Flashpix Version
                            value = ascii.GetString(pitem.Value).Trim('\0');
                            if (value == "0100")
                                value = "Flashpix Format Version 1.0";
                            else value = "reserved";
                            break;
                        case 0x9101: //ComponentsConfiguration
                            value = GetComponentsConfig(pitem.Value);
                            break;
                        case 0x927C: //MakerNote
                            value = ascii.GetString(pitem.Value).Trim('\0');
                            break;
                        case 0x9286: //UserComment
                            value = ascii.GetString(pitem.Value).Trim('\0');
                            break;
                        case 0x1B: //GPS Processing Method
                            value = ascii.GetString(pitem.Value).Trim('\0');
                            break;
                        case 0x1C: //GPS Area Info
                            value = ascii.GetString(pitem.Value).Trim('\0');
                            break;
                        default:
                            value = "-";
                            break;
                    }
                    #endregion
                }
                else if (pitem.Type == 0x9)
                {
                    #region 9 = SLONG (32-bit int)
                    value = BitConverter.ToInt32(pitem.Value, 0).ToString();
                    #endregion
                }
                else if (pitem.Type == 0xA)
                {
                    #region 10 = SRATIONAL (Two SLONGs, signed)

                    ExifHelper.GPSRational rat = new(pitem.Value);

                    value = pitem.Id switch
                    {
                        // ShutterSpeedValue
                        0x9201 => "1/" + Math.Round(Math.Pow(2, Convert.ToDouble(rat)), 2).ToString(),
                        // BrightnessValue
                        0x9203 => Math.Round(Convert.ToDouble(rat), 4).ToString(),
                        // ExposureBiasValue
                        0x9204 => Math.Round(Convert.ToDouble(rat), 2).ToString() + " eV",
                        _ => rat.ToString(),
                    };
                    #endregion
                }
                if (value.Length > 0)
                {
                    break;
                }
            }
            TagToAdd.Value = value;
            tags.Add(TagToAdd.Id, TagToAdd);
        }
    }

    ///

    IEnumerator IEnumerable.GetEnumerator() => tags.Values.GetEnumerator();
    public IEnumerator<ExifTag> GetEnumerator() => tags.Values.GetEnumerator();

    #endregion
}