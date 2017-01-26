class CreateTanks < ActiveRecord::Migration[5.0]
  def change
    create_table :tanks do |t|
      t.string :wgid
      t.boolean :ispremium
      t.string :name
      t.string :shortname
      t.integer :tier
      t.string :bigicon
      t.string :contouricon
      t.string :smallicon
	  t.references :nation, index: true, foreign_key: true
	  t.references :vehicle_type, index: true, foreign_key: true

      t.timestamps
    end
  end
end
