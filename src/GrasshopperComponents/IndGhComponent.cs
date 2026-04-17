using Grasshopper.Kernel;

namespace GrasshopperComponents
{
    public abstract class IndGhComponent : GH_Component
    {
        protected IndGhComponent(
            string name,
            string nickname,
            string description,
            string category,
            string subCategory)
            : base(name, nickname, description, category, subCategory)
        {
        }

        protected virtual bool IsDeveloperOnly => false;

        protected virtual GH_Exposure DefaultExposure => GH_Exposure.primary;

        public override GH_Exposure Exposure
        {
            get
            {
                if (IsDeveloperOnly && !ComponentVisibility.IsDeveloper)
                {
                    return GH_Exposure.hidden;
                }

                return DefaultExposure;
            }
        }
    }
}
