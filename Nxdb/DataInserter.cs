using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using org.basex.data;
using org.basex.index;
using org.basex.io;
using org.basex.util;


namespace Nxdb
{
    //Based mostly on Data.insert()
    //This will insert simple name/value pairs of a given node type or
    //will insert data from an XmlReader, which it will read from the current position ALL THE WAY to the end
    internal class DataInserter
    {
        //Multiple insert
        public static void Insert(XmlReader xmlReader, Data data, int ipre, int ipar)
        {
            Insert(xmlReader, data, ipre, ipar, null);
        }

        public static void Insert(XmlReader xmlReader, Data data, int ipre, int ipar, string documentName)
        {
            DataInserter dataInserter = new DataInserter(xmlReader, data, ipre, ipar, documentName);
            dataInserter.Run();
        }

        private DataInserter(XmlReader xmlReader, Data data, int ipre, int ipar, string documentName)
        {
            this.xmlReader = xmlReader;
            this.data = data;
            this.ipre = ipre;
            this.ipar = ipar;
            this.documentName = documentName;
        }

        //Single insert
        public static void Insert(XmlNodeType xmlNodeType, Data data, int ipre, int ipar, string insertName, string insertValue)
        {
            DataInserter dataInserter = new DataInserter(xmlNodeType, data, ipre, ipar, insertName, insertValue);
            dataInserter.Run();
        }

        private DataInserter(XmlNodeType xmlNodeType, Data data, int ipre, int ipar, string insertName, string insertValue)
        {
            //Do validity checks since a XmlReader won't be doing them for us
            if( xmlNodeType == XmlNodeType.Element
                || xmlNodeType == XmlNodeType.Attribute
                || xmlNodeType == XmlNodeType.ProcessingInstruction )
            {
                if( !XmlReader.IsName(insertName) )
                {
                    throw new XmlException("Invalid XML name");
                }
            }

            this.xmlNodeType = xmlNodeType;
            this.data = data;
            this.ipre = ipre;
            this.ipar = ipar;
            this.insertName = insertName;
            this.insertValue = insertValue;
        }

        //Multiple insert vars
        private readonly XmlReader xmlReader = null;
        private readonly string documentName = null;

        //Single insert vars
        private readonly XmlNodeType xmlNodeType = XmlNodeType.None;

        //Other inits
        private readonly Data data;
        private readonly int ipre;
        private readonly int ipar;

        private readonly Stack<int[]> preStack = new Stack<int[]>();     //Stores the pre values and their sizes as we descend: [pre][size]
        private readonly Queue<int[]> sizeQueue = new Queue<int[]>();    //Stores the finished pres and their respective sizes: [pre][size]
        private readonly List<byte[][]> attributes = new List<byte[][]>();

        private const int BUFFER_SIZE = IO.BLOCKSIZE >> IO.NODEPOWER;
        private int mpre = -1;  //The counter pre
        private int pre = -1;   //The current pre
        private int dis = -1;   //The current distance
        private string insertName = null;
        private string insertValue = null;
        
        private void UpdateVars()
        {
            //If we're updating, then we're preparing to insert, so increase the parent size
            if( preStack.Count > 0 )
            {
                preStack.Peek()[1]++;
            }

            mpre++;                                         //Increment the current counter pre
            pre = ipre + mpre;                              //The current insert pre is the initial insert pre + the counter
            dis = preStack.Count > 0                        //If not at the root, the distance is the counter parent to counter pre, otherwise initial parent to current pre
                ? pre - preStack.Peek()[0]
                : ipar >= 0 ? pre - ipar : 0;   

            //Check the buffer to make sure we're not full
            if (mpre != 0 && mpre % BUFFER_SIZE == 0)
            {
                data.insert(ipre + mpre - BUFFER_SIZE);
            }

            //Get the insert name and value
            if( xmlReader != null )
            {
                insertName = xmlReader.Name;
                insertValue = xmlReader.Value;
            }
        }

        private void OpenItem()
        {
            preStack.Push(new[] { pre, 1 });    //1 as initial size for self
            data._field_ns.open();
        }

        private void CloseItem()
        {
            int[] closingPre = preStack.Pop();
            sizeQueue.Enqueue(closingPre);
            data._field_ns.close(closingPre[0]);
            if( preStack.Count > 0 )
            {
                preStack.Peek()[1] += closingPre[1] - 1;    //Add the descendant size to the parent
            }
        }

        private void Run()
        {
            //Prepare
            data.meta.update();
            data.buffer(BUFFER_SIZE);

            //Document?
            if( !String.IsNullOrEmpty(documentName) )
            {
                UpdateVars();
                OpenItem();
                data.doc(pre, 0, Token.token(documentName));
                data.meta.ndocs++;
            }

            //Iterate
            if (xmlReader != null && xmlReader.ReadState == ReadState.Initial)
            {
                xmlReader.Read();
            }
            while (xmlReader == null || xmlReader.ReadState == ReadState.Interactive)
            {
                switch (xmlReader == null ? xmlNodeType : xmlReader.NodeType)
                {
                    case XmlNodeType.Attribute:
                        //Special attribute case only for single updates (attributes for multi-updates are handled in element)
                        if( xmlReader == null )
                        {
                            UpdateVars();

                            //Add the new attribute to the update buffer
                            byte[] attrName = Token.token(insertName);
                            data.attr(pre, dis, data.atts.index(attrName, null, false),
                                Token.token(insertValue), data._field_ns.uri(attrName, false), false);

                            //Explicitly increase the attribute size here
                            int ipark = data.kind(ipar);
                            data.attSize(ipar, ipark, data.attSize(ipar, ipark) + 1);
                        }
                        break;
                    case XmlNodeType.Element:
                        //Get the attributes (but only if this is a multi-insert)
                        bool namespaceFlag = false;
                        attributes.Clear();
                        if (xmlReader != null)
                        {
                            if (xmlReader.HasAttributes)
                            {
                                while (xmlReader.MoveToNextAttribute())
                                {
                                    if (xmlReader.Prefix == "xmlns")
                                    {
                                        data._field_ns.add(Token.token(xmlReader.LocalName), Token.token(xmlReader.Value), pre);
                                        namespaceFlag = true;
                                    }
                                    else if (xmlReader.Name == "xmlns")
                                    {
                                        data._field_ns.add(Token.EMPTY, Token.token(xmlReader.Value), pre);
                                        namespaceFlag = true;
                                    }
                                    else
                                    {
                                        attributes.Add(new[] { Token.token(xmlReader.Name), Token.token(xmlReader.Value) });
                                    }
                                }
                                xmlReader.MoveToElement();
                            }
                        }

                        //Element
                        UpdateVars();
                        OpenItem();
                        byte[] name = Token.token(insertName);
                        int tagIndex = data.tags.index(name, null, false);
                        int nsIndex = data._field_ns.uri(name, true);
                        data.elem(pre, dis, tagIndex, attributes.Count+1, 0, nsIndex, namespaceFlag);

                        //Add the attributes
                        foreach(byte[][] attribute in attributes)
                        {
                            UpdateVars();
                            int attrIndex = data.atts.index(attribute[0], null, false);
                            int attrNsIndex = data._field_ns.uri(attribute[0], false);
                            data.attr(pre, dis, attrIndex, attribute[1], attrNsIndex, false);
                        }

                        //Close if single or empty
                        if (xmlReader != null && xmlReader.IsEmptyElement)
                        {
                            CloseItem();
                        }

                        break;
                    case XmlNodeType.Comment:
                        //These conditionals are commented out because they prevent the inserter from properly handling
                        //whitespace in the top level of fragements
                        //They were originally added in case the reader passed through document-level whitespace which BaseX
                        //doesn't handle - not sure if that's still a problem...may need to revisit this
                        //if (xmlReader == null || xmlReader.Depth > 0)
                        //{
                            UpdateVars();
                            data.text(pre, dis, Token.token(insertValue), Data.COMM);
                        //}
                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Whitespace:
                        //if (xmlReader == null || xmlReader.Depth > 0)
                        //{
                            UpdateVars();
                            data.text(pre, dis, Token.token(insertValue), Data.TEXT);
                        //}
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        //if (xmlReader == null || xmlReader.Depth > 0)
                        //{
                            UpdateVars();
                            data.text(pre, dis, Token.token(insertName + " " + insertValue), Data.PI);
                        //}
                        break;
                    case XmlNodeType.EndElement:
                        CloseItem();
                        break;
                }

                //Break out if a single update, otherwise advance the reader
                if( xmlReader == null )
                {
                    break;
                }
                else
                {
                    xmlReader.Read();
                }
            }

            //Close any open documents (only 1 at most should be left)
            Debug.Assert(preStack.Count < 2);
            while(preStack.Count > 0)
            {
                CloseItem();
            }

            //Close out any remaining buffer
            if (data.bp != 0)
            {
                data.insert(ipre + mpre - (mpre % BUFFER_SIZE));    //The BaseX version has "mpre - 1" here, but they iterate one extra time so we don't need the -1
            }
            data.buffer(1);

            //Set inserted sizes
            while(sizeQueue.Count > 0)
            {
                int[] sizes = sizeQueue.Dequeue();
                data.size(sizes[0], Data.ELEM, sizes[1]);   //Treat all as Data.ELEM for now - will need to use proper kind if Data.size() changes
            }

            //Increase ancestor sizes
            if (ipar >= 0)
            {
                pre = ipar;
                mpre++;
                while (pre >= 0)
                {
                    int kind = data.kind(pre);
                    data.size(pre, kind, data.size(pre, kind) + mpre);
                    pre = data.parent(pre, kind);
                }
                data.updateDist(ipre + mpre, mpre);
            }

            //Delete old empty root node
            if(data.size(0, Data.DOC) == 1)
            {
                data.delete(0);
            }
        }
    }
}
