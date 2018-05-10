using System;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace StardewValley.MPSaveEditor.Models {
     public class SaveGame {
        XNamespace ns = "http://www.w3.org/2001/XMLSchema-instance";
        private XDocument _doc {get;set;}
        private XDocument _originalDoc {get; set;}
        private IEnumerable<XElement> _saveGame {get; set;}
        private string _path {get;set;}
        public SaveGame (string path) {
            try {
                _path = path;
                _doc = XDocument.Load(path);
                _originalDoc =XDocument.Load(path);

                _saveGame = _doc?.Element("SaveGame")?.Elements();                 
                if (_saveGame == null || !_saveGame.Any()) {
                    throw new Exception("Game file not parsed correctly");
                }
            } catch(Exception exception) {
                throw exception;
            }
        }

        public string FileName  => Path.GetFileName(_path);
        
        public XElement Host => _saveGame
            .First(x => x.Name == "player");
        
        public XElement Farm => _saveGame
            .First(x => x.Name == "locations")
            .Elements()
            .Single(x =>
                x.Attribute(ns + "type")?.Value == "Farm"); 
                
        public IEnumerable<XElement> Buildings => Farm
            .Element("buildings")
            .Elements();
        
        public IEnumerable<XElement> Cabins => Buildings
            .Where(x => 
                x.Element("indoors")?
                 .Attribute(ns + "type")?.Value == "Cabin"); 
                
        public IEnumerable<XElement> Farmhands => Cabins
            .Select(x => 
                x.Element("indoors")
                 .Element("farmhand"));
        
        public IEnumerable<string> FarmhandNames => Farmhands
        .Where(x => !x.Element("name").IsEmpty)
            .Select(x => 
                x.Element("name").Value);
        
        public void CreateNewCabin() {
            var cabin = new Cabin();
            cabin.CreateNewCabin();
            cabin.UpdateFarmhand(Host);
            MoveToValidLocation(cabin);
            Farm.Element("buildings").Add(cabin.Element);
        }
            
        public void SaveFile() {
            System.IO.Directory.CreateDirectory("saves");
            var dir = $"{FileName}_{DateTime.Now.ToString("MMddyyHHmm")}";
            System.IO.Directory.CreateDirectory($"saves\\{dir}");
            _originalDoc.Save($"./saves/{dir}/{FileName}_ORIGINAL");
            _doc.Save($"./saves/{dir}/{FileName}");
        }

        public void MoveToValidLocation(Cabin cabin) {
            var objects = new GameObjects(this);
            var valid = objects.MoveToValidLocation(cabin);
        }

        public XElement FindCabinByFarmhand(XElement farmhand) {
            return Cabins.FirstOrDefault(x => x.Element("indoors").Element("farmhand").Element("name").Value == farmhand.Element("name").Value);
        }

        public XElement GetFarmhandByName(string name) {
            return Farmhands.FirstOrDefault(x => x.Element("name").Value == name);
        }

        public void SwitchHost(XElement farmhand) {
            var host = new XElement(Host);
            farmhand = new XElement(farmhand);
            farmhand.Element("eventsSeen").ReplaceAll(host.Element("eventsSeen").Nodes());
            farmhand.Element("caveChoice").Value = host.Element("caveChoice").Value;
            var cabin = FindCabinByFarmhand(farmhand);
            Host.ReplaceAll(farmhand.Nodes());
            cabin.Element("indoors").Element("farmhand").ReplaceAll(host.Nodes());           
        }
    }   
}