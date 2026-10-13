using UnityEngine;

namespace DDay
{
    // Original transparent sprite sheets. Rectangles are import slices, not a baked office image.
    public static class DDayArt
    {
        static readonly Rect[] EmployeeRects = {
            new Rect(40, 0, 195, 717), new Rect(268, 0, 301, 717),
            new Rect(576, 0, 300, 717), new Rect(914, 0, 284, 717),
            new Rect(1244, 0, 259, 717), new Rect(1545, 0, 325, 717),
            new Rect(1891, 0, 295, 717)
        };
        static readonly int[] PortraitCenters = { 140, 418, 726, 1055, 1373, 1707, 2038 };
        static readonly int[] ReactionCenters = { 140, 403, 711, 1036, 1351, 1681, 2006 };
        // Transparent padding above the visible head in each seated slice (pixels from top).
        static readonly int[] SeatedHeadPadding = { 166, 190, 200, 218, 237, 175, 210 };
        public static float HeadPaddingWorld(int index) => SeatedHeadPadding[index] / 330f;
        static readonly Rect[] FacilityRects = {
            new Rect(55, 0, 610, 724), new Rect(820, 0, 490, 724),
            new Rect(1446, 0, 708, 724)
        };

        static Sprite[] employees;
        static Sprite[] facilities;
        static Sprite[] seated;
        static Sprite chair;
        static Sprite desk;
        static Sprite deskFront;
        static Sprite[] personalProps;

        static readonly Rect[] PropRects = {
            new Rect(20, 156, 310, 370), new Rect(360, 156, 205, 370),
            new Rect(560, 156, 315, 370), new Rect(900, 156, 295, 370),
            new Rect(1200, 156, 315, 370), new Rect(1515, 156, 315, 370),
            new Rect(1830, 156, 336, 370)
        };

        static readonly Rect[] SeatedRects = {
            new Rect(0, 0, 275, 725), new Rect(275, 0, 295, 725),
            new Rect(570, 0, 330, 725), new Rect(900, 0, 314, 725),
            new Rect(1214, 0, 281, 725), new Rect(1495, 0, 355, 725),
            new Rect(1850, 0, 319, 725)
        };

        public static Texture2D EmployeeSheet => Resources.Load<Texture2D>("DDay/PixelV2/Employees");
        public static Texture2D TaskReactionSheet =>
            Resources.Load<Texture2D>("DDay/PixelV2/TaskReactions");

        public static Rect TaskPortraitUv(int index, bool assigned)
        {
            Texture2D sheet = assigned ? TaskReactionSheet : EmployeeSheet;
            if (sheet == null) return Rect.zero;
            int[] centers = assigned ? ReactionCenters : PortraitCenters;
            return new Rect((centers[index] - 140f) / sheet.width,
                (sheet.height - 430f) / sheet.height,
                280f / sheet.width, 320f / sheet.height);
        }

        public static Sprite Employee(int index)
        {
            if (employees == null)
            {
                var sheet = EmployeeSheet;
                if (sheet == null) return null;
                employees = new Sprite[7];
                for (int i = 0; i < employees.Length; i++)
                    employees[i] = Sprite.Create(sheet, EmployeeRects[i], new Vector2(.5f, 0),
                        360f, 0, SpriteMeshType.FullRect);
            }
            return employees[index];
        }

        public static Rect EmployeeUv(int index)
        {
            var texture = EmployeeSheet;
            if (texture == null) return Rect.zero;
            var rect = EmployeeRects[index];
            return new Rect(rect.x / texture.width, rect.y / texture.height,
                rect.width / texture.width, rect.height / texture.height);
        }

        public static Sprite Facility(int index)
        {
            if (facilities == null)
            {
                var sheet = Resources.Load<Texture2D>("DDay/PixelV2/Facilities");
                if (sheet == null) return null;
                facilities = new Sprite[3];
                for (int i = 0; i < facilities.Length; i++)
                    facilities[i] = Sprite.Create(sheet, FacilityRects[i], new Vector2(.5f, 0),
                        300f, 0, SpriteMeshType.FullRect);
            }
            return facilities[index];
        }

        public static Sprite OfficeBackground => Resources.Load<Sprite>("DDay/OfficeTemplate/BackgroundFacilitiesV2");
        public static Texture2D SeatedSheet => Resources.Load<Texture2D>("DDay/OfficeTemplate/SeatedEmployees");

        public static Sprite SeatedEmployee(int index)
        {
            if (seated == null)
            {
                var sheet = SeatedSheet;
                if (sheet == null) return null;
                seated = new Sprite[7];
                for (int i = 0; i < 7; i++)
                    seated[i] = Sprite.Create(sheet, SeatedRects[i], new Vector2(.5f, 0),
                        330f, 0, SpriteMeshType.FullRect);
            }
            return seated[index];
        }

        public static Rect SeatedUv(int index)
        {
            var sheet = SeatedSheet;
            if (sheet == null) return Rect.zero;
            Rect rect = SeatedRects[index];
            return new Rect(rect.x / sheet.width, rect.y / sheet.height,
                rect.width / sheet.width, rect.height / sheet.height);
        }

        public static Sprite OfficeChair
        {
            get
            {
                if (chair == null)
                {
                    var sheet = Resources.Load<Texture2D>("DDay/OfficeTemplate/Workstation");
                    if (sheet != null) chair = Sprite.Create(sheet, new Rect(180, 107, 415, 610),
                        new Vector2(.5f, 0), 300f, 0, SpriteMeshType.FullRect);
                }
                return chair;
            }
        }

        public static Sprite OfficeDesk
        {
            get
            {
                if (desk == null)
                {
                    var sheet = Resources.Load<Texture2D>("DDay/OfficeTemplate/DeskFrontFacing");
                    if (sheet != null) desk = Sprite.Create(sheet, new Rect(150, 124, 1230, 775),
                        new Vector2(.5f, 0), 450f, 0, SpriteMeshType.FullRect);
                }
                return desk;
            }
        }

        public static Sprite OfficeDeskFront
        {
            get
            {
                if (deskFront == null)
                {
                    var sheet = Resources.Load<Texture2D>("DDay/OfficeTemplate/DeskFrontFacing");
                    if (sheet != null) deskFront = Sprite.Create(sheet, new Rect(150, 124, 1230, 450),
                        new Vector2(.5f, 0), 450f, 0, SpriteMeshType.FullRect);
                }
                return deskFront;
            }
        }

        public static Sprite PersonalProp(int index)
        {
            if (personalProps == null)
            {
                var sheet = Resources.Load<Texture2D>("DDay/OfficeTemplate/PersonalProps");
                if (sheet == null) return null;
                personalProps = new Sprite[7];
                for (int i = 0; i < 7; i++)
                    personalProps[i] = Sprite.Create(sheet, PropRects[i],
                        new Vector2(.5f, 0), 500f, 0, SpriteMeshType.FullRect);
            }
            return personalProps[index];
        }
    }
}
