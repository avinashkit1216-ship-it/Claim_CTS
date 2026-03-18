using System;
namespace Dependency
{
    public class Hammer
    {
        public void Use()
        {
            Console.WriteLine("Hammering Nails");
        }
    }

    public class Builder
    {
        private Hammer _hammer;
        private Saw _saw;
        public Builder()
        {
            _hammer = new Hammer();
        }

        public void BuildHouse()
        {
            _hammer.Use();
            Console.WriteLine();
        }
        

    }
}