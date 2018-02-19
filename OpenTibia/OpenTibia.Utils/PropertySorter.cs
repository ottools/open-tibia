#region Licence
/**
* Copyright (C) 2015 Open Tibia Tools <https://github.com/ottools/open-tibia>
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/
#endregion

#region Using Statements
using System;
using System.Collections;
using System.ComponentModel;
#endregion

namespace OpenTibia.Utils
{
    public class PropertySorter : ExpandableObjectConverter
    {
        #region | Public Methods |

        public override bool GetPropertiesSupported(ITypeDescriptorContext context) 
        {
            return true;
        }

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(value, attributes);
            ArrayList orderedProperties = new ArrayList();
            
            foreach (PropertyDescriptor pd in pdc)
            {
                Attribute attribute = pd.Attributes[typeof(PropertyOrderAttribute)];
                if (attribute != null)
                {
                    PropertyOrderAttribute poa = (PropertyOrderAttribute)attribute;
                    orderedProperties.Add(new PropertyOrderPair(pd.Name, poa.Order));
                }
                else
                {
                    orderedProperties.Add(new PropertyOrderPair(pd.Name, 0));
                }
            }

            orderedProperties.Sort();

            ArrayList propertyNames = new ArrayList();
            foreach (PropertyOrderPair pop in orderedProperties)
            {
                propertyNames.Add(pop.Name);
            }

            return pdc.Sort((string[])propertyNames.ToArray(typeof(string)));
        }

        #endregion
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class PropertyOrderAttribute : Attribute
    {
        #region | Constructor |

        public PropertyOrderAttribute(int order)
        {
            this.Order = order;
        }

        #endregion

        #region | Public Properties |

        public int Order { get; private set; }

        #endregion
    }

    public class PropertyOrderPair : IComparable
    {
        #region Private Properties

        private int order;

        #endregion

        #region | Contructor |

        public PropertyOrderPair(string name, int order)
        {
            this.order = order;
            this.Name = name;
        }

        #endregion

        #region | Public Properties |

        public string Name { get; private set; }

        #endregion

        #region | Public Methods |

        public int CompareTo(object obj)
        {
            int otherOrder = ((PropertyOrderPair)obj).order;

            if (otherOrder == this.order)
            {
                return string.Compare(this.Name, ((PropertyOrderPair)obj).Name);
            }
            else if (otherOrder > this.order)
            {
                return -1;
            }

            return 1;
        }

        #endregion
    }
}