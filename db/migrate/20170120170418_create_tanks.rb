class CreateTanks < ActiveRecord::Migration[5.0]
  def change
    create_table :tanks do |t|
      t.boolean :ispremium, null: false, default: false
      t.string :name, null: false
      t.string :shortname, null: false
      t.integer :tier, null: false, default: 0
	  t.integer :mark_count, null: false, default: 0
	  t.integer :moe_value, null: false, default: 0
      t.string :bigicon, null: false
      t.string :contouricon, null: false
      t.string :smallicon, null: false
	  t.references :nation, type: :string, index: true, null: false, foreign_key: true
	  t.references :vehicle_type, type: :string, index: true, null: false, foreign_key: true

      t.timestamps
    end
	
	change_column :tanks, :id, :integer
  end
end
