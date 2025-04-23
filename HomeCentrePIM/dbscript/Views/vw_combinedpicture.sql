
CREATE OR REPLACE VIEW public.vw_combined_picture
 AS
 SELECT p.uid,
    up.imageurl
   FROM product p
     JOIN uploadpicture up ON p.pictureid = up.id
  WHERE p.pictureid <> 0;

ALTER TABLE public.vw_combined_picture
    OWNER TO lmdev_admin;
