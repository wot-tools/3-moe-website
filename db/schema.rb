# This file is auto-generated from the current state of the database. Instead
# of editing this file, please use the migrations feature of Active Record to
# incrementally modify your database, and then regenerate this schema definition.
#
# Note that this schema.rb definition is the authoritative source for your
# database schema. If you need to create the application database on another
# system, you should be using db:schema:load, not running all the migrations
# from scratch. The latter is a flawed and unsustainable approach (the more migrations
# you'll amass, the slower it'll run and the greater likelihood for issues).
#
# It's strongly recommended that you check this file into your version control system.

ActiveRecord::Schema.define(version: 20170122000148) do

  create_table "clans", id: :integer, force: :cascade, options: "ENGINE=InnoDB DEFAULT CHARSET=utf8" do |t|
    t.string   "name",                            null: false
    t.string   "tag",                             null: false
    t.string   "cHex",        default: "#FFFFFF", null: false
    t.integer  "members",     default: 0,         null: false
    t.integer  "mark_count",  default: 0,         null: false
    t.integer  "moe_rating",  default: 0,         null: false
    t.datetime "updatedAtWG",                     null: false
    t.datetime "clanCreated",                     null: false
    t.string   "icon24px",                        null: false
    t.string   "icon32px",                        null: false
    t.string   "icon64px",                        null: false
    t.string   "icon195px",                       null: false
    t.string   "icon256px",                       null: false
    t.datetime "created_at",                      null: false
    t.datetime "updated_at",                      null: false
  end

  create_table "marks", force: :cascade, options: "ENGINE=InnoDB DEFAULT CHARSET=utf8" do |t|
    t.integer  "tank_id",    null: false
    t.integer  "player_id",  null: false
    t.datetime "created_at", null: false
    t.datetime "updated_at", null: false
    t.index ["player_id"], name: "index_marks_on_player_id", using: :btree
    t.index ["tank_id", "player_id"], name: "index_marks_on_tank_id_and_player_id", unique: true, using: :btree
    t.index ["tank_id"], name: "index_marks_on_tank_id", using: :btree
  end

  create_table "nations", id: :string, force: :cascade, options: "ENGINE=InnoDB DEFAULT CHARSET=utf8" do |t|
    t.string   "name",                   null: false
    t.integer  "mark_count", default: 0, null: false
    t.datetime "created_at",             null: false
    t.datetime "updated_at",             null: false
  end

  create_table "players", id: :integer, force: :cascade, options: "ENGINE=InnoDB DEFAULT CHARSET=utf8" do |t|
    t.string   "name",                                    null: false
    t.integer  "battles",                   default: 0,   null: false
    t.integer  "wn8",                       default: 0,   null: false
    t.integer  "wgrating",                  default: 0,   null: false
    t.float    "winratio",       limit: 24, default: 0.0, null: false
    t.integer  "mark_count",                default: 0,   null: false
    t.integer  "moe_rating",                default: 0,   null: false
    t.datetime "lastLogout",                              null: false
    t.datetime "lastBattle",                              null: false
    t.datetime "accountCreated",                          null: false
    t.datetime "updatedAtWG",                             null: false
    t.string   "clientLang",                              null: false
    t.integer  "clan_id"
    t.datetime "created_at",                              null: false
    t.datetime "updated_at",                              null: false
    t.index ["clan_id"], name: "index_players_on_clan_id", using: :btree
  end

  create_table "tanks", id: :integer, force: :cascade, options: "ENGINE=InnoDB DEFAULT CHARSET=utf8" do |t|
    t.boolean  "ispremium",       default: false, null: false
    t.string   "name",                            null: false
    t.string   "shortname",                       null: false
    t.integer  "mark_count",      default: 0,     null: false
    t.integer  "moe_value",       default: 0,     null: false
    t.string   "bigicon",                         null: false
    t.string   "contouricon",                     null: false
    t.string   "smallicon",                       null: false
    t.integer  "tier_id",                         null: false
    t.string   "nation_id",                       null: false
    t.string   "vehicle_type_id",                 null: false
    t.datetime "created_at",                      null: false
    t.datetime "updated_at",                      null: false
    t.index ["nation_id"], name: "index_tanks_on_nation_id", using: :btree
    t.index ["tier_id"], name: "index_tanks_on_tier_id", using: :btree
    t.index ["vehicle_type_id"], name: "index_tanks_on_vehicle_type_id", using: :btree
  end

  create_table "tiers", force: :cascade, options: "ENGINE=InnoDB DEFAULT CHARSET=utf8" do |t|
    t.string   "name",                   null: false
    t.integer  "mark_count", default: 0, null: false
    t.datetime "created_at",             null: false
    t.datetime "updated_at",             null: false
  end

  create_table "vehicle_types", id: :string, force: :cascade, options: "ENGINE=InnoDB DEFAULT CHARSET=utf8" do |t|
    t.string   "name",                   null: false
    t.integer  "mark_count", default: 0, null: false
    t.datetime "created_at",             null: false
    t.datetime "updated_at",             null: false
  end

  add_foreign_key "marks", "players"
  add_foreign_key "marks", "tanks"
  add_foreign_key "players", "clans"
  add_foreign_key "tanks", "nations"
  add_foreign_key "tanks", "tiers"
  add_foreign_key "tanks", "vehicle_types"
end
