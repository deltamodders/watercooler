x += 10
draw_self()
var col = draw_get_color()
draw_set_color(c_white)
draw_text(x - sprite_width / 2, y - 20, "i am john watercooler (custom object!!!)")
draw_set_color(col)
if (x > room_width * 1.5)
    x = -75