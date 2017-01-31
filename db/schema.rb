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

  create_table "articles", force: :cascade, options: "ENGINE=InnoDB DEFAULT CHARSET=utf8" do |t|
    t.string   "title"
    t.text     "text",       limit: 65535
    t.datetime "created_at",               null: false
    t.datetime "updated_at",               null: false
  end

  create_table "clans", id: :integer, force: :cascade, options: "ENGINE=InnoDB DEFAULT CHARSET=utf8" do |t|
    t.string   "name"
    t.string   "tag"
    t.string   "cHex"
    t.integer  "members"
    t.datetime "updatedAtWG"
    t.datetime "clanCreated"
    t.string   "icon24px"
    t.string   "icon32px"
    t.string   "icon64px"
    t.string   "icon195px"
    t.string   "icon256px"
    t.datetime "created_at",  null: false
    t.datetime "updated_at",  null: false
  end

  create_table "marks", force: :cascade, options: "ENGINE=InnoDB DEFAULT CHARSET=utf8" do |t|
    t.integer  "tank_id"
    t.integer  "player_id"
    t.datetime "created_at", null: false
    t.datetime "updated_at", null: false
    t.index ["player_id"], name: "index_marks_on_player_id", using: :btree
    t.index ["tank_id"], name: "index_marks_on_tank_id", using: :btree
  end

  create_table "nations", id: :string, force: :cascade, options: "ENGINE=InnoDB DEFAULT CHARSET=utf8" do |t|
    t.string   "name"
    t.datetime "created_at", null: false
    t.datetime "updated_at", null: false
  end

  create_table "players", id: :integer, force: :cascade, options: "ENGINE=InnoDB DEFAULT CHARSET=utf8" do |t|
    t.string   "name"
    t.integer  "battles"
    t.integer  "wgrating"
    t.float    "winratio",       limit: 24
    t.datetime "lastLogout"
    t.datetime "lastBattle"
    t.datetime "accountCreated"
    t.datetime "updatedAtWG"
    t.integer  "wn8"
    t.string   "clientLang"
    t.integer  "clan_id"
    t.datetime "created_at",                null: false
    t.datetime "updated_at",                null: false
    t.index ["clan_id"], name: "index_players_on_clan_id", using: :btree
  end

  create_table "tanks", id: :integer, force: :cascade, options: "ENGINE=InnoDB DEFAULT CHARSET=utf8" do |t|
    t.boolean  "ispremium"
    t.string   "name"
    t.string   "shortname"
    t.integer  "tier"
    t.string   "bigicon"
    t.string   "contouricon"
    t.string   "smallicon"
    t.string   "nation_id"
    t.string   "vehicle_type_id"
    t.datetime "created_at",      null: false
    t.datetime "updated_at",      null: false
    t.index ["nation_id"], name: "index_tanks_on_nation_id", using: :btree
    t.index ["vehicle_type_id"], name: "index_tanks_on_vehicle_type_id", using: :btree
  end

  create_table "vehicle_types", id: :string, force: :cascade, options: "ENGINE=InnoDB DEFAULT CHARSET=utf8" do |t|
    t.string   "name"
    t.datetime "created_at", null: false
    t.datetime "updated_at", null: false
  end

  add_foreign_key "marks", "players"
  add_foreign_key "marks", "tanks"
  add_foreign_key "players", "clans"
  add_foreign_key "tanks", "nations"
  add_foreign_key "tanks", "vehicle_types"
end
