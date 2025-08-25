//WATERCOOLER// REPLACE START gml_Object_obj_tester_Draw_64
draw_text_color(120, 120, "obj_tester!! BUT PATCHEDD", c_red, c_blue, c_green, c_white, 1);
scr_watercooler_demo();
//WATERCOOLER// REPLACE END

//WATERCOOLER// CREATE EVENT FROM-FUNCTION obj_tester Step
function obj_tester_Step() {
    draw_set_color(choose(c_white, c_red, c_blue, c_green));
}