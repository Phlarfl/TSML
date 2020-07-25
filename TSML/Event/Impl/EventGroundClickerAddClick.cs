using Placemaker;

namespace TSML.Event
{
    public class EventGroundClickerAddClick : Event
    {
        public GroundClicker GroundClicker { get; set; }

        public EventGroundClickerAddClick(GroundClicker groundClicker)
        {
            GroundClicker = groundClicker;
        }
    }
}
