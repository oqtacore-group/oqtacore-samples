/*
Copyright 2011, Andrew Polar

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pLSAtest
{
    class StopWordFilter
    {
        private string[] m_stopWords = null;
        private int m_nSize = 0;
		private HashTableDictionary _dict = new HashTableDictionary();
		
        public bool isThere(string word, int len)
        {
            return _dict.IsThere(word);
        }

        public StopWordFilter()
        {
            string s = "";
            s += "about a above across after again against all almost alone along ";
            s += "already also although always among an and another any anybody ";
            s += "0 1 2 3 4 5 6 7 8 9 ";
            s += "anyone anything anywhere are area areas around as ask asked asking ";
            s += "asks at away b back backed backing backs be became because become ";
            s += "becomes been before began behind being beings best better between ";
            s += "big both but by c came can cannot case cases certain certainly ";
            s += "clear clearly come could d did differ different differently do ";
            s += "does done down downed downing downs during e each early either ";
            s += "end ended ending ends enough even evenly ever every everybody ";
            s += "everyone everything everywhere f face faces fact facts far felt ";
            s += "few find finds first for four from full fully further furthered ";
            s += "furthering furthers g gave general generally get gets give given ";
            s += "gives go going good goods got great greater greatest group grouped ";
            s += "grouping groups h had has have having he her here herself high ";
            s += "higher highest him himself his how however i if important in ";
            s += "interest interested interesting interests into is it its itself ";
            s += "j just k keep keeps kind knew know known knows l large largely ";
            s += "last later latest least less let lets like likely long longer longest ";
            s += "m made make making man many may me member members men might more ";
            s += "most mostly mr mrs much must my myself n necessary need needed ";
            s += "needing needs never new new newer newest next no nobody non noone ";
            s += "not nothing now nowhere number numbers o of off often old older ";
            s += "oldest on once one only open opened opening opens or order ordered ";
            s += "ordering orders other others our out over p part parted parting ";
            s += "parts per perhaps place places point pointed pointing points possible ";
            s += "present presented presenting presents problem problems put puts q ";
            s += "quite r rather really right right room rooms s said same saw say ";
            s += "says second seconds see seem seemed seeming seems sees several shall ";
            s += "she should show showed showing shows side sides since small smaller ";
            s += "smallest so some somebody someone something somewhere state states ";
            s += "still such sure t take taken than that the their them then there ";
            s += "therefore these they thing things think thinks this those though ";
            s += "thought thoughts three through thus to today together too took toward ";
            s += "turn turned turning turns two u under until up upon us use used uses ";
            s += "v very w want wanted wanting wants was way ways we well wells went ";
            s += "were what when where whether which while who whole whose why will ";
            s += "with within without work worked working works would x y year years ";
            s += "yet you young younger youngest your z yours";
			s += "в и или а о на по ";
			s += "без более бы был была были было быть вам вас ведь весь вдоль вместо вне вниз внизу внутри во вокруг вот все всегда всего всех вы где да давай давать даже для до достаточно его ее её если есть ещё же за за исключением здесь из из-за или им иметь их как как-то кто когда кроме кто ли либо мне может мои мой мы на навсегда над надо наш не него неё нет ни них но ну об однако он она они оно от отчего очень по под после потому потому что почти при про снова со так также такие такой там те тем то того тоже той только том тут ты уже хотя чего чего-то чей чем что чтобы чьё чья эта эти это в и на нас мы с для по не при к и тоже также так как то что ";
            m_stopWords = s.Split(new char[]{' '});
			foreach(string word in m_stopWords)
			{
				_dict.GetWordIndex(word);
			}
            m_nSize = m_stopWords.Length;
        }
    }
}
