using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using org.basex.data;
using org.basex.util;
using Array=org.basex.util.Array;

namespace Nxdb
{
    /*
    //TODO: Queue changes and only execute at the very end in case there is parsing error while reading?

    internal class DataMatcher
    {
        private readonly Data data;

        private DataMatcher(Data data)
        {
            this.data = data;
        }

        public static void Match(XmlReader reader, Data data, int pre)
        {
            DataMatcher matcher = new DataMatcher(data);
            XmlReader subTreeReader = reader.ReadSubtree();
            subTreeReader.Read();
            matcher.Run(subTreeReader, pre);
        }

        //Returns whether modifications were made to the structure and thus callers should update pre references
        private bool Run(XmlReader xmlReader, int pre)
        {
            //Assumed they are both the same element, but double-check
            int kind = data.kind(pre);
            Debug.Assert(xmlReader.NodeType == XmlNodeType.Element && kind == Data.ELEM
                && xmlReader.Name == NxNode.GetName(data, pre, kind));

            //Match the attributes
            bool modifications = MatchAttributes(xmlReader, pre);

            //Match the children
            modifications |= MatchChildren(xmlReader, pre);

            return modifications;
        }

        //Returns whether modifications were made to the structure and thus callers should update pre references
        private bool MatchAttributes(XmlReader xmlReader, int pre)
        {
            //Get attributes from the database
            int[] attributes = NxNode.GetAttributePres(data, pre, Data.ELEM).ToArray();
            int attribute = 0;
            bool modified = false;

            //Match attributes
            while (xmlReader.MoveToNextAttribute() && attribute < attributes.Length)
            {
                //Check the names
                if (xmlReader.Name != NxNode.GetName(data, attributes[attribute], Data.ATTR))
                {
                    //Different name, scan forward to find a match
                    bool match = false;
                    for (int current = attribute+1; current < attributes.Length; current++)
                    {
                        if (NxNode.GetName(data, attributes[current], Data.ATTR) == xmlReader.Name)
                        {
                            //Found it, delete all before and set current attribute
                            for (int delete = current - 1; delete >= attribute; delete--)
                            {
                                NxNode.RemoveAttribute(data, attributes[attribute]);
                                modified = true;
                            }

                            //Update pres
                            int preDiff = current - attribute;
                            for (int c = current; c < attributes.Length; c++)
                            {
                                attributes[c] -= preDiff;
                            }

                            //Update indexes
                            attribute = current;
                            match = true;
                            break;
                        }
                    }

                    //If we didn't find it, it's an insert
                    if (!match)
                    {
                        NxNode.InsertAttributeBefore(data, pre, xmlReader.Name, xmlReader.Value, attributes[attribute]);

                        //Increment pres due to the insert
                        for (int c = attribute; c < attributes.Length; c++)
                        {
                            attributes[c]++;
                        }

                        modified = true;
                        continue;   //Don't increment the attribute counter if inserting, we need to try again for this attribute
                    }
                }

                //We had (or created) a match, so check for value equality
                if (xmlReader.Value != NxNode.GetValue(data, attributes[attribute], Data.ATTR))
                {
                    //Copy the reader value to the database
                    NxNode.SetValue(data, attributes[attribute], Data.ATTR, xmlReader.Value);
                }

                attribute++;
            }

            //Remove leftover unmatched attributes from the database
            for (int delete = attributes.Length - 1; delete >= attribute; delete--)
            {
                NxNode.RemoveAttribute(data, attributes[delete]);
                modified = true;
            }

            //Append any leftover from the reader
            while (xmlReader.MoveToNextAttribute())
            {
                NxNode.AppendAttribute(data, pre, Data.ELEM, xmlReader.Name, xmlReader.Value);
                modified = true;
            }

            //Move back to the element
            xmlReader.MoveToElement();

            return modified;
        }

        //Returns whether modifications were made to the structure and thus callers should update pre references
        private bool MatchChildren(XmlReader xmlReader, int pre)
        {
            //Get the children from the database
            int[] children = NxNode.GetChildPres(data, pre, Data.ELEM, false).ToArray();
            int child = 0;
            bool modified = false;

            //Loop through reader children
            while (xmlReader.Read() && child < children.Length)
            {
                //Ignore all but specific node types
                if (xmlReader.NodeType != XmlNodeType.Element
                    && xmlReader.NodeType != XmlNodeType.Text
                    && xmlReader.NodeType != XmlNodeType.Whitespace
                    && xmlReader.NodeType != XmlNodeType.SignificantWhitespace
                    && xmlReader.NodeType != XmlNodeType.Comment
                    && xmlReader.NodeType != XmlNodeType.ProcessingInstruction)
                {
                    continue;
                }

                //Get child pre/kind
                int childPre = children[child];
                int childKind = NxNode.GetKind(data, childPre);

                //Check types and names
                if (!NodeEquals(xmlReader, childPre, childKind))
                {
                    //Different type or name, scan forward to find a match
                    bool match = false;
                    for (int current = child+1; current < children.Length; current++)
                    {
                        if (NodeEquals(xmlReader, children[current], NxNode.GetKind(data, children[current])))
                        {
                            //Found it, delete all before
                            int currentPre = children[current];
                            int currentId = data.id(currentPre);
                            for (int delete = current - 1; delete >= child ; delete--)
                            {
                                NxNode.RemoveChild(data, children[delete]);
                                modified = true;
                            }

                            //Update pres
                            children[current] = data.pre(currentId);
                            int preDiff = currentPre - children[current];   //old - new
                            for (int c = current + 1; c < children.Length; c++ )
                            {
                                children[c] -= preDiff;
                            }

                            //Update indexes
                            child = current;
                            childPre = children[child];
                            childKind = NxNode.GetKind(data, childPre);
                            match = true;
                            break;
                        }
                    }

                    //If we didn't find it, it's an insert
                    if (!match)
                    {
                        if (xmlReader.NodeType == XmlNodeType.Element)
                        {
                            int childId = data.id(childPre);
                            NxNode.InsertBefore(data, pre, xmlReader.ReadSubtree(), childPre);

                            //Update pres due to the insert
                            children[child] = data.pre(childId);
                            int preDiff = children[child] - childPre;   //new - old
                            for(int c = child + 1 ; c < children.Length ; c++ )
                            {
                                children[c] += preDiff;
                            }
                        }
                        else
                        {
                            NxNode.InsertBefore(data, pre, xmlReader.NodeType, xmlReader.Name, xmlReader.Value, childPre);

                            //Increment pres due to the insert
                            for(int c = child ; c < children.Length ; c++)
                            {
                                children[c]++;
                            }
                        }
                        modified = true;
                        continue;   //Don't increment the child counter if inserting, we need to try again for this node
                    }
                }

                //We had (or created) a match, so check for value equality
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    //...descend into the subtree if an element
                    XmlReader subTreeReader = xmlReader.ReadSubtree();
                    subTreeReader.Read();   //Advance to the parent element
                    int nextId = child+1 < children.Length ? data.id(children[child+1]) : -1;
                    if( Run(subTreeReader, childPre) )
                    {
                        modified = true;

                        //Increment/decrement all subsiquent pres due to any modifications
                        if (nextId != -1)
                        {
                            int nextPre = data.pre(nextId);
                            int preDiff = nextPre - children[child + 1];    //new - old
                            children[child + 1] = nextPre;
                            for (int c = child + 2; c < children.Length; c++)
                            {
                                children[c] += preDiff;
                            }
                        }
                    }
                }
                else
                {
                    //...otherwise, check the values
                    if (xmlReader.Value != NxNode.GetValue(data, childPre, childKind))
                    {
                        NxNode.SetValue(data, childPre, childKind, xmlReader.Value);
                    }
                }

                child++;
            }

            //Remove leftover unmatched children from the database
            for (int delete = children.Length - 1; delete >= child ; delete--)
            {
                NxNode.RemoveChild(data, children[delete]);
                modified = true;
            }

            //Append any leftover from the reader
            if(xmlReader.ReadState == ReadState.Initial)
            {
                xmlReader.Read();
            }
            while (xmlReader.ReadState == ReadState.Interactive)
            {
                //Scan forward until/if we get to a valid starting point
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    XmlReader subTreeReader = xmlReader.ReadSubtree();
                    subTreeReader.Read();   //Advance to the parent element
                    NxNode.AppendChild(data, pre, Data.ELEM, subTreeReader);
                    modified = true;
                }
                else if(xmlReader.NodeType == XmlNodeType.Text
                    || xmlReader.NodeType == XmlNodeType.Whitespace
                    || xmlReader.NodeType == XmlNodeType.SignificantWhitespace
                    || xmlReader.NodeType == XmlNodeType.Comment
                    || xmlReader.NodeType == XmlNodeType.ProcessingInstruction)
                {
                    NxNode.AppendChild(data, pre, Data.ELEM, xmlReader.NodeType, xmlReader.Name, xmlReader.Value);
                    modified = true;
                }
                xmlReader.Read();
            }

            return modified;
        }

        //Check nodes for identity equality - for text-based node types, this returns true since only the value is different
        private bool NodeEquals(XmlReader xmlReader, int pre, int kind)
        {
            switch (kind)
            {
                case Data.ELEM:
                    return xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == NxNode.GetName(data, pre, kind);
                case Data.TEXT:
                    return xmlReader.NodeType == XmlNodeType.Text
                        || xmlReader.NodeType == XmlNodeType.Whitespace
                        || xmlReader.NodeType == XmlNodeType.SignificantWhitespace;
                case Data.COMM:
                    return xmlReader.NodeType == XmlNodeType.Comment;
                case Data.PI:
                    return xmlReader.NodeType == XmlNodeType.ProcessingInstruction;
            }
            return false;
        }
    }
    */
}
