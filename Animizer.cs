/*
    Animizer is a tool built to structure animations,
    created primarily to make XNA/MonoGame sprite animation
    easier but can be used for generic use.

    The files are organized as sets of Anims that contain
    animation info. Anims reference outside images,
    primarily sprite sheets but could also achieve
    animations with individual images if desired.

    Files are stored in XML format, to keep the files easy
    to read and parse. (I'm a fan of human readability.)

    ~ Fauxtrot, 2018
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;


namespace AnimizerApi {

    /// <summary>
    /// Organizes animation frames (AnimFrames) into a list
    /// with a name associated with them.
    /// </summary>
    public struct Anim {
        public string id;
        public List<AnimFrame> frames;
    }

    /// <summary>
    /// Contains all info about a frame of animation.
    /// 
    /// The frame contains info about location and size on
    /// an associated image (used for sprite sheets), and a
    /// time, measured in frames/rate, for displaying the frame.
    /// </summary>
    public struct AnimFrame {
        public float centerX;
        public float centerY;
        public float sizeX;
        public float sizeY;
        public float timeFrames;
        public float timeRate;
        public string image;
    }

    /// <summary>
    /// A class full of tools to pack and unpack AnimSets
    /// from .animset files.
    /// </summary>
    public static class Animizer {

        /// <summary>
        /// Creates a .animset file at the given path and
        /// name.
        /// 
        /// Overwrites the file if it exists already.
        /// </summary>
        public static void CreateAnimSet(
                Dictionary<string, Anim> anims, string path, string filename) {

            if (!filename.EndsWith(".animset"))
                filename += ".animset";

            XmlWriter writer = XmlWriter.Create(Path.Combine(path, filename),
                new XmlWriterSettings {
                    Indent = true,
                    IndentChars = "  "
                });

            writer.WriteStartDocument();
            writer.WriteStartElement("animset");

            // Collect all unique images assigned to each anim frame,
            // and tie them to a attribute and id linking back.
            // This leaves AnimFrames to their own images without
            // filling the file with duplicate image info, and
            // also provides a neat list of files to Load through.

            List<string> images = new List<string>();
            writer.WriteStartElement("images");

            foreach (var animKV in anims) {
                foreach (AnimFrame frame in animKV.Value.frames) {
                    if (images.Contains(frame.image))
                        continue;

                    images.Add(frame.image);
                    int index = images.Count-1;

                    writer.WriteStartElement("image");
                    writer.WriteAttributeString("id", index.ToString());
                    writer.WriteAttributeString("source", 
                        GetRelativePath(frame.image, path));
                    writer.WriteEndElement();
                }
            }

            writer.WriteEndElement();

            foreach (var animKV in anims) {
                string animId = animKV.Key;
                Anim anim = animKV.Value;

                writer.WriteStartElement("anim");
                writer.WriteAttributeString("id", animId);

                foreach (AnimFrame frame in anim.frames) {
                    string center = string.Format(
                        "{0},{1}",
                        frame.centerX, frame.centerY);
                    string size = string.Format(
                        "{0},{1}",
                        frame.sizeX, frame.sizeY);
                    string imageIndex = images.IndexOf(frame.image).ToString();

                    writer.WriteStartElement("frame");
                    writer.WriteAttributeString("center", center);
                    writer.WriteAttributeString("size", size);
                    writer.WriteAttributeString("timeFrames", frame.timeFrames.ToString());
                    writer.WriteAttributeString("timeRate", frame.timeRate.ToString());
                    writer.WriteAttributeString("imageid", imageIndex);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            //writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Close();
        }

        /// <summary>
        /// Reads a .animset file and returns a list of
        /// Anims from it.
        /// </summary>
        public static Dictionary<string, Anim> ReadAnimSet(
                string path, string filename) {

            Dictionary<string, Anim> output = new Dictionary<string, Anim>();
            Dictionary<int, string> images = new Dictionary<int, string>();
            
            string thisAnim = "";
            Anim thisAnimObj = new Anim();

            XmlReader reader = XmlReader.Create(Path.Combine(path, filename));

            while (reader.Read()) {
                if (reader.NodeType == XmlNodeType.Element) {
                    switch (reader.Name) {
                        case "image":
                            // Turn relative path into absolute and add
                            Uri imageUri = new Uri(Path.Combine(path,
                                reader.GetAttribute("source")));
                            images.Add(
                                int.Parse(reader.GetAttribute("id")), 
                                Path.GetFullPath(imageUri.LocalPath));
                            break;
                        case "anim":
                            // Create new Anim. Next several elements are going to be frames.
                            thisAnim = reader.GetAttribute("id");
                            thisAnimObj = new Anim();
                            thisAnimObj.id = thisAnim;
                            thisAnimObj.frames = new List<AnimFrame>();
                            break;
                        case "frame":
                            if (thisAnim == "")
                                throw new XmlException("XML element out of order!");

                            string[] center = reader.GetAttribute("center").Split(',');
                            string[] size = reader.GetAttribute("size").Split(',');

                            AnimFrame frame = new AnimFrame() {
                                centerX = int.Parse(center[0]),
                                centerY = int.Parse(center[1]),
                                sizeX = int.Parse(size[0]),
                                sizeY = int.Parse(size[1]),
                                timeFrames = int.Parse(reader.GetAttribute("timeFrames")),
                                timeRate = int.Parse(reader.GetAttribute("timeRate")),
                                image = images[int.Parse(reader.GetAttribute("imageid"))]
                            };

                            thisAnimObj.frames.Add(frame);
                            break;
                    }
                }
                if (reader.NodeType == XmlNodeType.EndElement) {
                    if (reader.Name.Equals("anim") && thisAnim != "") {
                        output.Add(thisAnim, thisAnimObj);
                        thisAnim = "";
                    }
                }
            }

            reader.Close();

            return output;
        }

        // https://stackoverflow.com/a/703292
        /// <summary>
        /// Formats a relative path for a specified file,
        /// with respect to a given directory.
        /// </summary>
        public static string GetRelativePath(string filespec, string folder) {
            Uri pathUri = new Uri(filespec);

            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
                folder += Path.DirectorySeparatorChar;

            Uri folderUri = new Uri(folder);
            
            return Uri.UnescapeDataString(
                folderUri.MakeRelativeUri(pathUri)
                .ToString()
                .Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
