class CreateClans < ActiveRecord::Migration[5.0]
  def change
    create_table :clans do |t|
      t.string :name
      t.string :tag
      t.string :cHex
      t.integer :members
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
