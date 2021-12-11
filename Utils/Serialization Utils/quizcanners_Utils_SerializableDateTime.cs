using System;
using UnityEngine;

namespace QuizCanners.Utils
{
    [Serializable]
    public class SerializableDateTime
    {
        [SerializeField] private bool _isSet;
        [SerializeField] private long _ticks;

        [NonSerialized] private bool _dateTimeValidUpdated;
        [NonSerialized] private DateTime _dateTime;

        public bool IsSet => _isSet;

        public DateTime Value
        {
            get
            {
                if (!_isSet)
                {
                    Debug.LogError("Trying to get invalid Date Time");
                    return DateTime.Now;
                }

                if (!_dateTimeValidUpdated)
                {
                    _dateTime = new DateTime(ticks: _ticks);
                    _dateTimeValidUpdated = true;
                }

                return _dateTime;
            }

            set
            {
                _dateTime = value;
                _ticks = value.Ticks;
                _isSet = true;
                _dateTimeValidUpdated = true;
            }
        }

        public static implicit operator DateTime(SerializableDateTime d) => d.Value;

        public static implicit operator SerializableDateTime(DateTime d)
        {
            var val = new SerializableDateTime
            {
                Value = d
            };
            return val;
        }

    }

}