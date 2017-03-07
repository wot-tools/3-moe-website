class CreateClans < ActiveRecord::Migration[5.0]
  def change
    create_table :clans do |t|
      t.string :name, null: false
      t.string :tag, null: false
      t.string :cHex, null: false, default: "#FFFFFF"
      t.integer :members, null: false, default: 0
	  t.integer :mark_count, null: false, default: 0
	  t.integer :moe_rating, null: false, default: 0
      t.datetime :updatedAtWG
	  t.datetime :clanCreated
      t.string :icon24px
      t.string :icon32px
      t.string :icon64px
      t.string :icon195px
      t.string :icon256px

      t.timestamps
    end
	
	change_column :clans, :id, :integer
  end
end
