using Placemaker;

namespace TSML.Event
{
    public class EventGroundClickerRemoveClick : Event
    {
        public GroundClicker GroundClicker { get; set; }

        public EventGroundClickerRemoveClick(GroundClicker groundClicker)
        {
            GroundClicker = groundClicker;
        }
    }
}
