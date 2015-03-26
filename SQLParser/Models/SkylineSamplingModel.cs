namespace prefSQL.SQLParser.Models
{
    using System.Collections.Generic;

    public class SkylineSamplingModel
    {
        private readonly List<string> _skylineAttributes;
        private readonly int _randomSubsetsCount;
        private readonly int _randomSubsetsDimension;
        private readonly string _preSkylineAttributesSqlString;
        private readonly string _postSkylineAttributesSqlString;
        private readonly List<string> _skylineOperators; 

        public List<string> SkylineAttributes
        {
            get { return _skylineAttributes; }
        }

        public int RandomSubsetsCount
        {
            get { return _randomSubsetsCount; }
        }

        public int RandomSubsetsDimension
        {
            get { return _randomSubsetsDimension; }
        }

        public string PreSkylineAttributesSqlString
        {
            get { return _preSkylineAttributesSqlString; }
        }
        
        public string PostSkylineAttributesSqlString
        {
            get { return _postSkylineAttributesSqlString; }
        }

        public List<string> SkylineOperators
        {
            get { return _skylineOperators; }
        }

        public SkylineSamplingModel(int randomSubsetsCount, int randomSubsetsDimension, string preSkylineAttributesSqlString, string postSkylineAttributesSqlString)
        {
            _randomSubsetsCount = randomSubsetsCount;
            _randomSubsetsDimension = randomSubsetsDimension;
            _preSkylineAttributesSqlString = preSkylineAttributesSqlString;
            _postSkylineAttributesSqlString = postSkylineAttributesSqlString;
            _skylineAttributes = new List<string>();
            _skylineOperators = new List<string>();
        }
    }
}